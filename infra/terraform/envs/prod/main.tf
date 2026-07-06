terraform {
  required_version = ">= 1.9"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    # Values supplied via -backend-config in CI (bucket/key/region/dynamodb_table differ
    # per environment); omitted here so this file has no environment-specific state.
  }
}

provider "aws" {
  region = var.region
}

locals {
  name = "ticket-triage-${var.environment}"
  tags = {
    Project     = "ticket-triage"
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}

module "vpc" {
  source             = "../../modules/vpc"
  name               = local.name
  availability_zones = var.availability_zones
  tags               = local.tags
}

module "sqs" {
  source      = "../../modules/sqs"
  name_prefix = local.name
  queue_names = ["tickets-inbox", "triage-inbox", "notifications-inbox", "reporting-inbox"]
  tags        = local.tags
}

module "secrets" {
  source      = "../../modules/secrets"
  name_prefix = local.name
  secret_names = [
    "jwt-signing-key",
    "openai-api-key",
    "anthropic-api-key",
    "gemini-api-key",
    "db-credentials"
  ]
  tags = local.tags
}

module "rds" {
  source          = "../../modules/rds"
  name            = local.name
  vpc_id          = module.vpc.vpc_id
  subnet_ids      = module.vpc.public_subnet_ids
  vpc_cidr_block  = var.vpc_cidr_block
  instance_class  = "db.t4g.micro"
  master_password = var.db_master_password
  tags            = local.tags
}

// Skipped by default per the plan's cost guidance — only stand this up when the
// caching behavior (Add-on B) is specifically being demoed.
module "redis" {
  count          = var.enable_redis_cache ? 1 : 0
  source         = "../../modules/elasticache"
  name           = local.name
  vpc_id         = module.vpc.vpc_id
  subnet_ids     = module.vpc.public_subnet_ids
  vpc_cidr_block = var.vpc_cidr_block
  tags           = local.tags
}

module "api" {
  source           = "../../modules/ecs-service"
  name             = "${local.name}-api"
  region           = var.region
  vpc_id           = module.vpc.vpc_id
  subnet_ids       = module.vpc.public_subnet_ids
  image            = var.api_image
  use_fargate_spot = false # prod: on-demand, not Spot
  desired_count    = 2     # prod: two tasks for basic availability

  # NOTE: the DB password is interpolated into a plain environment variable here for
  # brevity. Before a real deploy, switch this to the ECS task definition's `secrets`
  # field pulling from module.secrets so the password never lands in plan/state output.
  environment = merge(
    {
      ASPNETCORE_ENVIRONMENT             = "Production"
      "ConnectionStrings__Tickets"       = "Host=${module.rds.endpoint};Database=ticket_triage;Username=ticket_triage;Password=${var.db_master_password}"
      "ConnectionStrings__Identity"      = "Host=${module.rds.endpoint};Database=ticket_triage;Username=ticket_triage;Password=${var.db_master_password}"
      "ConnectionStrings__Notifications" = "Host=${module.rds.endpoint};Database=ticket_triage;Username=ticket_triage;Password=${var.db_master_password}"
      "ConnectionStrings__Reporting"     = "Host=${module.rds.endpoint};Database=ticket_triage;Username=ticket_triage;Password=${var.db_master_password}"
      "Sqs__Queues__TicketsInbox"        = module.sqs.queue_urls["tickets-inbox"]
      "Sqs__Queues__NotificationsInbox"  = module.sqs.queue_urls["notifications-inbox"]
      "Sqs__Queues__ReportingInbox"      = module.sqs.queue_urls["reporting-inbox"]
      "Sqs__Routes__TicketCreated__0"    = module.sqs.queue_urls["triage-inbox"]
      "Sqs__Routes__TicketCreated__1"    = module.sqs.queue_urls["reporting-inbox"]
      "Sqs__Routes__TicketResolved__0"   = module.sqs.queue_urls["notifications-inbox"]
      "Sqs__Routes__TicketResolved__1"   = module.sqs.queue_urls["reporting-inbox"]
    },
    var.enable_redis_cache ? { "ConnectionStrings__Redis" = "${module.redis[0].primary_endpoint}:${module.redis[0].port}" } : {}
  )

  secret_arns    = values(module.secrets.secret_arns)
  sqs_queue_arns = values(module.sqs.queue_arns)
  tags           = local.tags
}

# Triage runs as its own Fargate service (see docs/adr/006) — it consumes triage-inbox and
# publishes TicketTriaged/TicketTriageFailed, with no direct traffic from the ALB/frontend,
# so it needs no load balancer wiring of its own, just the same cluster/subnet/SQS access.
module "triage_service" {
  source           = "../../modules/ecs-service"
  name             = "${local.name}-triage-service"
  region           = var.region
  vpc_id           = module.vpc.vpc_id
  subnet_ids       = module.vpc.public_subnet_ids
  image            = var.triage_service_image
  use_fargate_spot = false # prod: on-demand, not Spot
  desired_count    = 2     # prod: two tasks for basic availability

  environment = merge(
    {
      ASPNETCORE_ENVIRONMENT               = "Production"
      "ConnectionStrings__Triage"          = "Host=${module.rds.endpoint};Database=ticket_triage;Username=ticket_triage;Password=${var.db_master_password}"
      "Otel__ServiceName"                  = "TicketTriage.TriageService"
      "Sqs__Queues__TriageInbox"           = module.sqs.queue_urls["triage-inbox"]
      "Sqs__Routes__TicketTriaged__0"      = module.sqs.queue_urls["tickets-inbox"]
      "Sqs__Routes__TicketTriaged__1"      = module.sqs.queue_urls["notifications-inbox"]
      "Sqs__Routes__TicketTriaged__2"      = module.sqs.queue_urls["reporting-inbox"]
      "Sqs__Routes__TicketTriageFailed__0" = module.sqs.queue_urls["tickets-inbox"]
      "Sqs__Routes__TicketTriageFailed__1" = module.sqs.queue_urls["reporting-inbox"]
    },
    var.enable_redis_cache ? { "ConnectionStrings__Redis" = "${module.redis[0].primary_endpoint}:${module.redis[0].port}" } : {}
  )

  secret_arns    = values(module.secrets.secret_arns)
  sqs_queue_arns = values(module.sqs.queue_arns)
  tags           = local.tags
}

module "frontend" {
  source      = "../../modules/frontend"
  bucket_name = "${local.name}-frontend"
  tags        = local.tags
}
