INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`)
VALUES (@p0, @p1, @p2, @p3)
ON DUPLICATE KEY UPDATE `Name` = COALESCE (IF ((@xTotal := `Total`) IS NOT NULL, NULL, NULL), IF (@xTotal > @p6, @p4, `Name`)), `Total` = IF (@xTotal > @p6, ( `Total` + @p5 ), `Total`)
