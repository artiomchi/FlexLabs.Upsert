INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`)
VALUES (@p0, @p1, @p2, @p3)
ON DUPLICATE KEY UPDATE `Name` = ( CASE WHEN ( VALUES(`Name`) IS NULL ) THEN @p4 ELSE VALUES(`Name`) END )
