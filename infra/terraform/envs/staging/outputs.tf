output "frontend_url" {
  value = "https://${module.frontend.distribution_domain_name}"
}

output "ecs_cluster_id" {
  value = module.api.cluster_id
}
