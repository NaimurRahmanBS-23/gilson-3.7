IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[FNS_FeedManager_ProductLoadAll]') AND type in (N'P', N'PC'))
DROP PROCEDURE [FNS_FeedManager_ProductLoadAll]
GO
IF  EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[Tri_Insert_Product_FeedManager]'))
DROP TRIGGER [dbo].[Tri_Insert_Product_FeedManager]
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[FNS_FeedManager_CopyFromFroogle]') AND type in (N'P', N'PC'))
DROP PROCEDURE [FNS_FeedManager_CopyFromFroogle]
GO
