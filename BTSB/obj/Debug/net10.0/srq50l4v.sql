CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `Kunjungan` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nama` longtext NOT NULL,
    `Telepon` longtext NOT NULL,
    `Instansi` longtext NOT NULL,
    `Email` longtext NOT NULL,
    `Alamat` longtext NOT NULL,
    `Keperluan` longtext NOT NULL,
    `DetailKeperluan` longtext NULL,
    `WaktuDatang` datetime(6) NOT NULL,
    `WaktuSelesai` datetime(6) NULL,
    `Durasi` int NULL,
    `Status` longtext NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `Users` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nama` varchar(100) NOT NULL,
    `Username` varchar(50) NOT NULL,
    `PasswordHash` longtext NOT NULL,
    `Role` varchar(20) NOT NULL,
    `Status` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

CREATE TABLE `VisitNotifications` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `KunjunganId` int NOT NULL,
    `Judul` longtext NOT NULL,
    `Pesan` longtext NOT NULL,
    `Dibaca` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261001033554_InitialCreate', '10.0.12');

COMMIT;

