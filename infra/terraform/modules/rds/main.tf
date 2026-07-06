# Single-AZ db.t4g.micro Postgres — cheapest managed option. For a pure demo session,
# consider running Postgres in a Fargate task instead to skip RDS's per-hour cost
# entirely (see infra/README.md); this module exists for environments that want the
# managed-backup/failover story instead.

resource "aws_db_subnet_group" "this" {
  name       = "${var.name}-db-subnets"
  subnet_ids = var.subnet_ids
  tags       = var.tags
}

resource "aws_security_group" "db" {
  name_prefix = "${var.name}-db-"
  vpc_id      = var.vpc_id

  # Scoped to the VPC's CIDR rather than the ECS task's security group: referencing the
  # task SG here would make this module depend on ecs-service while ecs-service's own
  # environment needs this module's endpoint, i.e. a dependency cycle. VPC-CIDR scoping
  # keeps both modules independent while still not exposing Postgres publicly.
  ingress {
    description = "Postgres from within the VPC only"
    from_port   = 5432
    to_port     = 5432
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

resource "aws_db_instance" "this" {
  identifier             = "${var.name}-db"
  engine                 = "postgres"
  engine_version         = "16"
  instance_class         = var.instance_class
  allocated_storage      = 20
  db_name                = var.database_name
  username               = var.master_username
  password               = var.master_password
  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false
  skip_final_snapshot    = true
  deletion_protection    = false # deliberate: this env is torn down after each demo session

  tags = var.tags
}
