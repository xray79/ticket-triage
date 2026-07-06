# Single-node cache.t4g.micro Redis — cheapest ElastiCache shape. Per the plan's cost
# guidance, skip this module entirely for early-stage demos; it's only worth provisioning
# once the caching behavior (Add-on B) is specifically being demoed.

resource "aws_elasticache_subnet_group" "this" {
  name       = "${var.name}-cache-subnets"
  subnet_ids = var.subnet_ids
  tags       = var.tags
}

resource "aws_security_group" "cache" {
  name_prefix = "${var.name}-cache-"
  vpc_id      = var.vpc_id

  # Scoped to the VPC's CIDR rather than a specific task's security group, for the same
  # reason as the RDS module: avoids an inter-module dependency cycle with ecs-service.
  ingress {
    description = "Redis from within the VPC only"
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr_block]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = var.tags
}

resource "aws_elasticache_cluster" "this" {
  cluster_id         = "${var.name}-cache"
  engine             = "redis"
  engine_version     = "7.1"
  node_type          = var.node_type
  num_cache_nodes    = 1
  port               = 6379
  subnet_group_name  = aws_elasticache_subnet_group.this.name
  security_group_ids = [aws_security_group.cache.id]

  tags = var.tags
}
