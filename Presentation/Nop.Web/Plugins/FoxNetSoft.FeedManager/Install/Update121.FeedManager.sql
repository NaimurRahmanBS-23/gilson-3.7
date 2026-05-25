--add a new column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedRec]') and NAME='PriceMarkup')
BEGIN
	ALTER TABLE [FNS_FeedRec]
	ADD [PriceMarkup] int NOT NULL CONSTRAINT DF_FNS_FeedRec_PriceMarkup DEFAULT 0
END
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedProduct]') and NAME='ASIN')
BEGIN
	ALTER TABLE [FNS_FeedProduct] DROP CONSTRAINT DF_FNS_FeedProduct_ASIN
	ALTER TABLE [FNS_FeedProduct] DROP COLUMN [ASIN]
END
GO
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'FNS_FeedASIN') AND type in (N'U'))
BEGIN
	CREATE TABLE [FNS_FeedASIN](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[ProductId] [int] NOT NULL,
		[ASIN] [nvarchar](10) NULL
	PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'FNS_FeedEPID') AND type in (N'U'))
BEGIN
	CREATE TABLE [FNS_FeedEPID](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[ProductId] [int] NOT NULL,
		[EPID] [nvarchar](15) NULL
	PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
	) ON [PRIMARY]
END
GO
update ScheduleTask
set Seconds=60*15
where Name='Feed Manager' and Seconds=43200
GO
--add a new column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedRec]') and NAME='UseStaticFileName')
BEGIN
	ALTER TABLE [FNS_FeedRec]
	ADD [UseStaticFileName] bit NOT NULL CONSTRAINT DF_FNS_FeedRec_UseStaticFileName DEFAULT 0
END
GO
--add a new column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedRec]') and NAME='ExportGrouped')
BEGIN
	ALTER TABLE [FNS_FeedRec]
	ADD [ExportGrouped] bit NOT NULL CONSTRAINT DF_FNS_FeedRec_ExportGrouped DEFAULT 0
END
GO
--add a new column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedRec]') and NAME='ExportProductCombinations')
BEGIN
	ALTER TABLE [FNS_FeedRec]
	ADD [ExportProductCombinations] bit NOT NULL CONSTRAINT DF_FNS_FeedRec_ExportProductCombinations DEFAULT 0
END
GO
--add a new column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=object_id('[FNS_FeedRec]') and NAME='SkipMainProduct')
BEGIN
	ALTER TABLE [FNS_FeedRec]
	ADD [SkipMainProduct] bit NOT NULL CONSTRAINT DF_FNS_FeedRec_SkipMainProduct DEFAULT 0
END
GO
