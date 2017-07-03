CREATE TABLE [dbo].[SASSensor] (
	[msgId]      NCHAR (128) NOT NULL,
    [deviceId]   TEXT       NOT NULL,
    [temp]       REAL       NOT NULL,
    [accelx]     REAL       NOT NULL,
    [accely]     REAL       NOT NULL,
    [accelz]     REAL       NOT NULL,
    [time]       DATETIME   NOT NULL,
    [Longitude]  REAL       NULL,
    [Latitude]   REAL       NULL,
	PRIMARY KEY CLUSTERED ([msgId] ASC)
);