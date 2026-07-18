CREATE TABLE `customers` (
    `id` BIGINT AUTO_INCREMENT NOT NULL,
    `email` VARCHAR(255) NOT NULL,
    `balance` DECIMAL(12,2) DEFAULT 0 NOT NULL,
    CONSTRAINT `pk_customers` PRIMARY KEY (`id`),
    CONSTRAINT `uq_customers_email` UNIQUE (`email`)
);
