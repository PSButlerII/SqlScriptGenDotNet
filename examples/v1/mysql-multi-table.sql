CREATE TABLE `customers` (
    `id` BIGINT AUTO_INCREMENT NOT NULL,
    CONSTRAINT `pk_customers` PRIMARY KEY (`id`)
);

CREATE TABLE `orders` (
    `id` BIGINT NOT NULL,
    `customer_id` BIGINT NOT NULL,
    CONSTRAINT `fk_orders_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE CASCADE
);

CREATE TABLE `audit_alpha` (
    `id` BIGINT NOT NULL,
    `external_user_id` BIGINT,
    CONSTRAINT `fk_audit_external` FOREIGN KEY (`external_user_id`) REFERENCES `external_users` (`id`)
);

CREATE TABLE `audit_beta` (
    `id` BIGINT NOT NULL
);
