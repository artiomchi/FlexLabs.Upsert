INSERT INTO `TestEntity` (`ID`, `Name`, `Status`, `Total`)
VALUES (@p0, @p1, @p2, @p3)
ON DUPLICATE KEY UPDATE `Name` = IF (( `Total` > @p5 ) AND ( `Status` != VALUES(`Status`) ), @p4, `Name`)
