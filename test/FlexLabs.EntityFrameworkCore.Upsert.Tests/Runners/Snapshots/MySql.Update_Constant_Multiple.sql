INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`)
VALUES (@p0, @p1, @p2, @p3), (@p4, @p5, @p6, @p7)
ON DUPLICATE KEY UPDATE `Name` = @p8
