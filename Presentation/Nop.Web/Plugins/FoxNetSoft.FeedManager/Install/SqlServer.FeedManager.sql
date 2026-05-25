IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FNS_FeedManager_ProductLoadAll]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[FNS_FeedManager_ProductLoadAll]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[FNS_FeedManager_ProductLoadAll]
(
	@FeedId		int,	
	@AllowedCustomerRoleIds	nvarchar(MAX) = null,	--a list of customer role IDs (comma-separated list) for which a product should be shown (if a subjet to ACL)
	@PageIndex			int = 0, 
	@PageSize			int = 2147483644
)
AS
BEGIN
SET NOCOUNT ON
DECLARE @StoreId int,@LanguageId int
select @StoreId=StoreId,@LanguageId=LanguageId from FNS_FeedRec WITH (NOLOCK) where Id=@FeedId
if @LanguageId>0
begin	
	declare @countLanguage int
	set @countLanguage=0
	select @countLanguage=count(*) from [Language] L WITH (NOLOCK) where L.Published=1
	if @countLanguage=1
	begin
		set @LanguageId=0
	end
end

DECLARE @sql nvarchar(max)

	CREATE TABLE #ExportProductsTmp
	(
		[ProductId] int NOT NULL,
		[ProductTypeId] int NOT NULL,
		[VisibleIndividually] bit NOT NULL,
		[ParentGroupedProductId] int NOT NULL
	)
	
	--filter by customer role IDs (access control list)
	SET @AllowedCustomerRoleIds = isnull(@AllowedCustomerRoleIds, '')	
	CREATE TABLE #FilteredCustomerRoleIds
	(
		CustomerRoleId int not null
	)
	INSERT INTO #FilteredCustomerRoleIds (CustomerRoleId)
	SELECT CAST(data as int) FROM [nop_splitstring_to_table](@AllowedCustomerRoleIds, ',')

		--product name
		SET @sql = 'INSERT INTO #ExportProductsTmp (ProductId,ProductTypeId,VisibleIndividually,ParentGroupedProductId)
		SELECT p.Id, p.ProductTypeId, p.VisibleIndividually, p.ParentGroupedProductId
		FROM Product p with (NOLOCK)
		WHERE ((p.VisibleIndividually = 1 AND p.Id in (select ProductId from Product_Category_Mapping WITH (NOLOCK))) 
			OR p.ParentGroupedProductId!=0)
			AND p.Published = 1
			AND p.Deleted = 0
			AND (getutcdate() BETWEEN ISNULL(p.AvailableStartDateTimeUtc, ''1/1/1900'') and ISNULL(p.AvailableEndDateTimeUtc, ''1/1/2999''))'
	
	--show hidden and ACL
		SET @sql = @sql + '
		AND (p.SubjectToAcl = 0 OR EXISTS (
			SELECT 1 FROM #FilteredCustomerRoleIds [fcr]
			WHERE
				[fcr].CustomerRoleId IN (
					SELECT [acl].CustomerRoleId
					FROM [AclRecord] acl with (NOLOCK)
					WHERE [acl].EntityId = p.Id AND [acl].EntityName = ''Product''
				)
			))'
	
	--show hidden and filter by store
	IF @StoreId > 0
	BEGIN
		SET @sql = @sql + '
		AND (p.LimitedToStores = 0 OR EXISTS (
			SELECT 1 FROM [StoreMapping] sm with (NOLOCK)
			WHERE [sm].EntityId = p.Id AND [sm].EntityName = ''Product'' and [sm].StoreId=' + CAST(@StoreId AS nvarchar(max)) + '
			))'
	END

	EXEC sp_executesql @sql
drop table #FilteredCustomerRoleIds

if exists(select * from FNS_FeedCondition C WITH (NOLOCK) where C.FeedId=@FeedId)
begin
	DECLARE @sqlCondition nvarchar(max)
	declare @FeedConditionId int,@ConditionGroupId int,@ConditionTypeId int,@ConditionPropertyId int,@ConditionOperatorId int,@ConditionValueId int
	DECLARE View_FeedCondition INSENSITIVE CURSOR
	FOR SELECT Id,ConditionGroupId,ConditionTypeId,ConditionPropertyId,ConditionOperatorId,ConditionValueId
	FROM FNS_FeedCondition WITH (NOLOCK)
	where FeedId=@FeedId
	FOR READ ONLY

	OPEN View_FeedCondition
	FETCH NEXT FROM View_FeedCondition into @FeedConditionId,@ConditionGroupId,@ConditionTypeId,@ConditionPropertyId,@ConditionOperatorId,@ConditionValueId
	WHILE @@Fetch_Status=0
	begin
		set @sqlCondition=''
		--Product
		if @ConditionTypeId=1
		begin
			--Category
			if @ConditionPropertyId=1
			begin
				--Equal To
				if @ConditionOperatorId=0
					set @sqlCondition=@sqlCondition+' ProductId not in '
				else	
					set @sqlCondition=@sqlCondition+' ProductId in '
					--Not Equal To
					--@ConditionOperatorId=1
					
				set @sqlCondition=@sqlCondition+' (select SM.ProductId
						from Product_Category_Mapping SM
						where SM.CategoryId='+str(@ConditionValueId)+')
							and (ParentGroupedProductId=0 or 
								(ParentGroupedProductId>0 and ParentGroupedProductId not in (select ProductId from #ExportProductsTmp))
								)'
			end
			--Manufacturer
			if @ConditionPropertyId=2
			begin
				--Equal To
				if @ConditionOperatorId=0
					set @sqlCondition=@sqlCondition+' ProductId not in '
				else	
					set @sqlCondition=@sqlCondition+' ProductId in '
					--Not Equal To
					--@ConditionOperatorId=1
					
				set @sqlCondition=@sqlCondition+' (select SM.ProductId
						from Product_Manufacturer_Mapping SM
						where SM.ManufacturerId='+str(@ConditionValueId)+')
							and (ParentGroupedProductId=0 or 
								(ParentGroupedProductId>0 and ParentGroupedProductId not in (select ProductId from #ExportProductsTmp))
								)'
			end	
			
			if @ConditionPropertyId in (3,4,5,6,9)
			begin
				set @sqlCondition=@sqlCondition+' ProductId in (select SM.Id
					from Product SM
					where '
				--Price
				if @ConditionPropertyId=3
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and Price'
				--Quantity
				if @ConditionPropertyId=4
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and StockQuantity'
				--PreOrder
				if @ConditionPropertyId=5
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and AvailableForPreOrder'
				--FreeShipping
				if @ConditionPropertyId=6
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and IsFreeShipping'
				--HasGtin
				if @ConditionPropertyId=9
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and (CASE WHEN len(ltrim(rtrim(NULLIF(Gtin,''''))))>0 THEN 1 ELSE 0 END)'
				--Weight
				if @ConditionPropertyId=10
					set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and Weight'

				if @ConditionOperatorId=0
					set @sqlCondition=@sqlCondition+'!='
				if @ConditionOperatorId=1	
					set @sqlCondition=@sqlCondition+'='
				if @ConditionOperatorId=2	
					set @sqlCondition=@sqlCondition+'>='
				if @ConditionOperatorId=3	
					set @sqlCondition=@sqlCondition+'<='
				if @ConditionOperatorId=4	
					set @sqlCondition=@sqlCondition+'>'
				if @ConditionOperatorId=5	
					set @sqlCondition=@sqlCondition+'<'

				set @sqlCondition=@sqlCondition+str(@ConditionValueId)+')'
			end	
		end
		--ProductSpecificationAttribute
		if @ConditionTypeId=2
		begin
			--Equal To
			if @ConditionOperatorId=0
				set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and ProductId not in '
			else	
				set @sqlCondition=@sqlCondition+' ProductTypeId!=10 and ProductId in '
				--Not Equal To
				--@ConditionOperatorId=1
				
			set @sqlCondition=@sqlCondition+' (select SM.ProductId
					from Product_SpecificationAttribute_Mapping SM
					where SM.SpecificationAttributeOptionId='+str(@ConditionValueId)+')'
		end
		if len(@sqlCondition)>0
		begin
	 		SET @sqlCondition='delete from #ExportProductsTmp where '+@sqlCondition
			--print @sqlCondition
			EXEC sp_executesql @sqlCondition
		end	
		FETCH NEXT FROM View_FeedCondition into @FeedConditionId,@ConditionGroupId,@ConditionTypeId,@ConditionPropertyId,@ConditionOperatorId,@ConditionValueId
	end	
	DEALLOCATE View_FeedCondition
end

--delete empty grouped products
delete from #ExportProductsTmp where ProductTypeId=10 and ProductId not in (select ParentGroupedProductId from #ExportProductsTmp)

--delete subproducts (product variants) with parent product
delete from #ExportProductsTmp 
where ProductTypeId=5 and VisibleIndividually=0 and ParentGroupedProductId!=0 
	and ParentGroupedProductId not in (select ProductId from #ExportProductsTmp where ProductTypeId=10)

--Delete Non export products
CREATE TABLE #tmpFeedDefault (CategoryId int, ParentCategoryId int, ProductId int, Levelsort int, FeedCategoryId int, CategoryMap nvarchar(1000) null, DefaultValuesXML nvarchar(max) null, TypeActiveId int)

--FNS_FeedCategory
if exists(select * from FNS_FeedCategory F WITH (NOLOCK) where F.FeedId=@FeedId)
begin
	--Category tree
	;with cte_tree (CategoryId, ParentCategoryId, Levelsort, FeedCategoryId, CategoryMap, DefaultValuesXML, TypeActiveId) as 
	(
		select C.Id as CategoryId, C.ParentCategoryId, 
			1 as Levelsort,
			isnull(F.CategoryId, 0) as FeedCategoryId,
			isnull(F.CategoryMap, convert(nvarchar(1000),null)) as CategoryMap,
			isnull(F.DefaultValuesXML, convert(nvarchar(max),null)) as DefaultValuesXML,
			isnull(F.TypeActiveId, 0) as TypeActiveId
		from Category C WITH (NOLOCK)
			OUTER APPLY (select FC.* from FNS_FeedCategory FC WITH (NOLOCK) where C.Id=FC.CategoryId and FC.FeedId=@FeedId) as F
		where C.ParentCategoryId=0 and C.Deleted=0 --and C.Published=1
		union all
		select T.Id as CategoryId,T.ParentCategoryId as ParentCategoryId,
			C.Levelsort+1 as Levelsort,
			isnull(F.CategoryId, C.FeedCategoryId) as FeedCategoryId,
			isnull(F.CategoryMap, C.CategoryMap) as CategoryMap,
			isnull(F.DefaultValuesXML, C.DefaultValuesXML) as DefaultValuesXML,
			case 
				when F.TypeActiveId is null or F.TypeActiveId=0 then C.TypeActiveId
				else F.TypeActiveId
			end as TypeActiveId
		from cte_tree C,Category T WITH (NOLOCK)
			OUTER APPLY (select FC.* from FNS_FeedCategory FC WITH (NOLOCK) where T.Id=FC.CategoryId and FC.FeedId=@FeedId) as F
		where C.CategoryId=T.ParentCategoryId and T.Deleted=0 --and T.Published=1
	)

	insert  into #tmpFeedDefault (CategoryId, ParentCategoryId, ProductId , Levelsort, FeedCategoryId, CategoryMap, DefaultValuesXML, TypeActiveId)
	select TOP 1 WITH TIES SM.CategoryId, isnull(C.ParentCategoryId,0), SM.ProductId , isnull(C.Levelsort,1), isnull(C.FeedCategoryId,0), C.CategoryMap, C.DefaultValuesXML, C.TypeActiveId
	from Product_Category_Mapping SM WITH (NOLOCK) 
		left join cte_tree C on SM.CategoryId=C.CategoryId
	where SM.ProductId in (select ProductId from #ExportProductsTmp)
		and SM.CategoryId in (select Id from Category WITH (NOLOCK) where Deleted=0)
	ORDER BY ROW_NUMBER() OVER(PARTITION BY SM.ProductId 
			ORDER BY  
				case 
					when ISNULL(C.TypeActiveId,0)=0 then 0
					when ISNULL(C.TypeActiveId,0)=1 then -1
					when ISNULL(C.TypeActiveId,0)=2 then 2
				end ASC, SM.DisplayOrder)
end

--FNS_FeedProduct
if exists(select * from FNS_FeedProduct F WITH (NOLOCK) where F.FeedId=@FeedId)
begin
	insert  into #tmpFeedDefault (CategoryId, ParentCategoryId, ProductId , Levelsort, FeedCategoryId, CategoryMap, DefaultValuesXML, TypeActiveId)
	select 0 as CategoryId, 0 as ParentCategoryId, F.ProductId,0 as Levelsort,0 as FeedCategoryId, F.CategoryMap, F.DefaultValuesXML, F.TypeActiveId
	from FNS_FeedProduct F WITH (NOLOCK)
	where F.FeedId=@FeedId
end

--No Export
delete from #ExportProductsTmp where ProductId in (select ProductId from #tmpFeedDefault where TypeActiveId=2)
delete from #tmpFeedDefault where TypeActiveId=2

--delete subproducts (product variants) with parent product
delete from #ExportProductsTmp 
where ProductTypeId=5 and VisibleIndividually=0 and ParentGroupedProductId!=0 
	and ParentGroupedProductId not in (select ProductId from #ExportProductsTmp where ProductTypeId=10)

--paging
DECLARE @PageLowerBound int
DECLARE @PageUpperBound int
DECLARE @RowsToReturn int
SET @RowsToReturn = @PageSize * (@PageIndex + 1)	
SET @PageLowerBound = @PageSize * @PageIndex
SET @PageUpperBound = @PageLowerBound + @PageSize + 1

	CREATE TABLE #PageIndex 
	(
		[IndexId] int IDENTITY (1, 1) NOT NULL,
		[ProductId] int NOT NULL
	)
	INSERT INTO #PageIndex ([ProductId])
	SELECT ProductId
	FROM #ExportProductsTmp
	ORDER BY ProductId
	
	--return products
	create table #ExportProducts (ProductId int Not null) 
	insert into #ExportProducts (ProductId)
	select TOP (@RowsToReturn) [pi].ProductId
	FROM
		#PageIndex [pi]
	WHERE
		[pi].IndexId > @PageLowerBound AND 
		[pi].IndexId < @PageUpperBound
	ORDER BY
		[pi].IndexId
	
	DROP TABLE #PageIndex
	
select P.*
from Product P WITH (NOLOCK)
where P.Id in (select ProductId from #ExportProducts)
ORDER BY P.[Name] ASC

select P.ProductId , P.Levelsort, P.FeedCategoryId, P.CategoryMap, P.DefaultValuesXML
from #tmpFeedDefault P 
where P.ProductId in (select ProductId from #ExportProducts)	
order by P.ProductId , P.Levelsort

drop table #tmpFeedDefault

If (@LanguageId>0)
begin
	select L.EntityId as ProductId,ltrim(rtrim(Substring(L.LocaleValue,1,400))) as Name
	from LocalizedProperty L WITH (NOLOCK) 
	where L.EntityId in (select ProductId from #ExportProducts) and L.LanguageId=@LanguageId and L.LocaleKeyGroup='Product' and L.LocaleKey='Name'
		and ltrim(rtrim(ISNULL(L.LocaleValue,'')))!=''

	select L.EntityId as ProductId,L.LocaleValue as ShortDescription
	from LocalizedProperty L WITH (NOLOCK) 
	where L.EntityId in (select ProductId from #ExportProducts) and L.LanguageId=@LanguageId and L.LocaleKeyGroup='Product' and L.LocaleKey='ShortDescription'
		and ltrim(rtrim(ISNULL(L.LocaleValue,'')))!=''
					
	select L.EntityId as ProductId,L.LocaleValue as FullDescription
	from LocalizedProperty L WITH (NOLOCK) 
	where L.EntityId in (select ProductId from #ExportProducts) and L.LanguageId=@LanguageId and L.LocaleKeyGroup='Product' and L.LocaleKey='FullDescription'
		and ltrim(rtrim(ISNULL(L.LocaleValue,'')))!=''
end
else
begin
	select TOP 0 ProductId,'' as Name
	from #ExportProducts
	
	select TOP 0 ProductId,'' as ShortDescription
	from #ExportProducts

	select TOP 0 ProductId,'' as FullDescription
	from #ExportProducts
end

--UrlRecord
create table #UrlRecord	(ProductId int,SeName nvarchar(400))

insert into #UrlRecord (ProductId,SeName)
select TOP 1 WITH TIES U.EntityId as ProductId,ltrim(rtrim(Substring(U.Slug,1,400))) as SeName
from UrlRecord U WITH (NOLOCK)
where U.EntityId in (select ProductId from #ExportProducts) 
	and U.EntityName='Product' and U.LanguageId=0 and U.IsActive=1	
ORDER BY ROW_NUMBER() OVER(PARTITION BY U.EntityId ORDER BY U.Id DESC)
			
If (@LanguageId>0)
begin
	update #UrlRecord
	set SeName=ltrim(rtrim(Substring(US.Slug,1,400)))
	from #UrlRecord C, (select TOP 1 WITH TIES U.EntityId,U.Slug
		from UrlRecord U WITH (NOLOCK)
		where U.EntityId in (select ProductId from #UrlRecord) 
			and U.EntityName='Product' and U.LanguageId=@LanguageId and U.IsActive=1
			and ltrim(rtrim(U.Slug))!=''
		ORDER BY ROW_NUMBER() OVER(PARTITION BY U.EntityId ORDER BY U.Id DESC)) US
	where C.ProductId=US.EntityId				
end
	
select * from  #UrlRecord
drop table #UrlRecord
--end UrlRecord

--Category
If (@LanguageId>0)
begin
	SELECT TOP 1 WITH TIES PM.ProductId,case 
			when L.LocaleValue IS NULL then C.Name
			when ltrim(rtrim(L.LocaleValue))=''  then C.Name
			else L.LocaleValue
			end as Name
	FROM Product_Category_Mapping PM WITH (NOLOCK),Category C WITH (NOLOCK)
		left join LocalizedProperty L on C.Id=L.EntityId 
					and L.LocaleKeyGroup='Category' 
					and L.LocaleKey='Name' 
					and L.LanguageId=@LanguageId	
	WHERE PM.CategoryId=C.Id and PM.ProductId in (select ProductId from #ExportProducts)
		AND C.Deleted=0 and C.Published=1
		AND (@StoreId = 0 or C.LimitedToStores = 0 OR EXISTS (
			SELECT 1 FROM [StoreMapping] sm with (NOLOCK)
			WHERE [sm].EntityId = C.Id AND [sm].EntityName = 'Category' and [sm].StoreId=@StoreId))
	ORDER BY ROW_NUMBER() OVER(PARTITION BY PM.ProductId ORDER BY PM.DisplayOrder ASC)
end
else
begin
	SELECT TOP 1 WITH TIES PM.ProductId,C.Name
	FROM Product_Category_Mapping PM WITH (NOLOCK),Category C WITH (NOLOCK)
	WHERE PM.CategoryId=C.Id and PM.ProductId in (select ProductId from #ExportProducts)
		AND C.Deleted=0 and C.Published=1
		AND (@StoreId = 0 or C.LimitedToStores = 0 OR EXISTS (
			SELECT 1 FROM [StoreMapping] sm with (NOLOCK)
			WHERE [sm].EntityId = C.Id AND [sm].EntityName = 'Category' and [sm].StoreId=@StoreId))
	ORDER BY ROW_NUMBER() OVER(PARTITION BY PM.ProductId ORDER BY PM.DisplayOrder ASC)
end

--end Category

--Manufacturer
If (@LanguageId>0)
begin
	SELECT TOP 1 WITH TIES PM.ProductId,case 
			when L.LocaleValue IS NULL then M.Name
			when ltrim(rtrim(L.LocaleValue))=''  then M.Name
			else L.LocaleValue
			end as Name
	FROM Product_Manufacturer_Mapping PM WITH (NOLOCK),Manufacturer M WITH (NOLOCK)
		left join LocalizedProperty L on M.Id=L.EntityId 
					and L.LocaleKeyGroup='Manufacturer' 
					and L.LocaleKey='Name' 
					and L.LanguageId=@LanguageId	
	WHERE PM.ManufacturerId=M.Id and PM.ProductId in (select ProductId from #ExportProducts)
			AND M.Deleted=0 and M.Published=1
	ORDER BY ROW_NUMBER() OVER(PARTITION BY PM.ProductId ORDER BY PM.DisplayOrder ASC)
end
else
begin
	SELECT TOP 1 WITH TIES PM.ProductId,M.Name
	FROM Product_Manufacturer_Mapping PM WITH (NOLOCK),Manufacturer M WITH (NOLOCK)
	WHERE PM.ManufacturerId=M.Id and PM.ProductId in (select ProductId from #ExportProducts)
			AND M.Deleted=0 and M.Published=1
	ORDER BY ROW_NUMBER() OVER(PARTITION BY PM.ProductId ORDER BY PM.DisplayOrder ASC)
end
--end Manufacturer

--Picture
	SELECT PM.ProductId,PM.DisplayOrder,P.Id,P.IsNew,P.MimeType,P.SeoFilename
	FROM Product_Picture_Mapping PM WITH (NOLOCK),Picture P WITH (NOLOCK)
	WHERE PM.PictureId=P.Id and PM.ProductId in (select ProductId from #ExportProducts)
	ORDER BY PM.ProductId,PM.DisplayOrder ASC
--end Picture

--Product Specification
create table #Product_SpecificationAttribute_Mapping (ProductId int, AttributeTypeId int, SpecificationAttributeOptionId int, CustomValue nvarchar(4000), ShowOnProductPage bit)

insert into #Product_SpecificationAttribute_Mapping (ProductId, AttributeTypeId, SpecificationAttributeOptionId, CustomValue, ShowOnProductPage)
select PM.ProductId, PM.AttributeTypeId, PM.SpecificationAttributeOptionId,PM.CustomValue,PM.ShowOnProductPage
from Product_SpecificationAttribute_Mapping PM WITH(NOLOCK, INDEX(IX_PSAM_ProductId))
where PM.ProductId in (select ProductId from #ExportProducts)

If (@LanguageId>0)
begin
	select distinct S.Id,PM.ProductId,
		case 
			when PM.CustomValue is not null and ltrim(rtrim(PM.CustomValue))!='' then PM.CustomValue collate SQL_Latin1_General_CP1_CI_AS
			when LSO.LocaleValue IS NULL or ltrim(rtrim(LSO.LocaleValue))='' then SO.Name collate SQL_Latin1_General_CP1_CI_AS
			else LSO.LocaleValue collate SQL_Latin1_General_CP1_CI_AS
			end as Value
	from SpecificationAttribute S WITH (NOLOCK),
		SpecificationAttributeOption SO WITH(NOLOCK)
			left join LocalizedProperty LSO WITH(NOLOCK) on SO.Id=LSO.EntityId 
					and LSO.LocaleKeyGroup='SpecificationAttributeOption' 
					and LSO.LocaleKey='Name' 
					and LSO.LanguageId=@LanguageId, 
		#Product_SpecificationAttribute_Mapping PM
	where S.Id=SO.SpecificationAttributeId 
		and SO.Id=PM.SpecificationAttributeOptionId
		and PM.AttributeTypeId in (0,10) --3.50
end
else
begin
	select distinct S.Id,PM.ProductId,
		case 
			when PM.CustomValue is not null and ltrim(rtrim(PM.CustomValue))!='' then PM.CustomValue collate SQL_Latin1_General_CP1_CI_AS
			else SO.Name collate SQL_Latin1_General_CP1_CI_AS
		end as Value
	from SpecificationAttribute S WITH (NOLOCK), 
		SpecificationAttributeOption SO WITH(NOLOCK), 
		#Product_SpecificationAttribute_Mapping PM
	where S.Id=SO.SpecificationAttributeId 
		and SO.Id=PM.SpecificationAttributeOptionId
		and PM.AttributeTypeId in (0,10) --3.50
end

drop table #Product_SpecificationAttribute_Mapping
--end Product Specification

--Product External Ids
select P.ProductId, A.[ASIN],E.[EPID] 
from #ExportProducts P 
	left join FNS_FeedASIN A WITH (NOLOCK) on P.ProductId=A.ProductId
	left join FNS_FeedEPID E WITH (NOLOCK) on P.ProductId=E.ProductId
--end Product Product External Ids

drop table #ExportProducts
END
GO
IF  EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[Tri_Insert_Product_FeedManager]'))
DROP TRIGGER [dbo].[Tri_Insert_Product_FeedManager]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[Tri_Insert_Product_FeedManager] ON [dbo].[Product]
FOR INSERT
AS
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FNS_FeedProduct]') AND type in (N'U'))
		and EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FNS_FeedRec]') AND type in (N'U'))
begin
	insert into FNS_FeedProduct (FeedId,ProductId,CategoryMap,DefaultValuesXML,TypeActiveId)
	select F.Id,I.Id,null,null,2
	from FNS_FeedRec F WITH (NOLOCK),inserted I
	where F.DoNotExportNewProducts=1
end		
GO
EXEC sp_settriggerorder @triggername=N'[dbo].[Tri_Insert_Product_FeedManager]', @order=N'Last', @stmttype=N'INSERT'
GO

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FNS_FeedManager_CopyFromFroogle]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[FNS_FeedManager_CopyFromFroogle]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[FNS_FeedManager_CopyFromFroogle]
(
	@FeedId		int
)
AS
BEGIN
SET NOCOUNT ON
	insert into FNS_FeedProduct (FeedId,ProductId,CategoryMap,DefaultValuesXML,TypeActiveId)
	select @FeedId,G.ProductId,G.Taxonomy,
		'<DefaultValues>'+
			+case
			when ltrim(rtrim(G.Gender))!='' then '<DefaultValue><FeedAttributeId>17</FeedAttributeId><Value>'+ltrim(rtrim(G.Gender))+'</Value></DefaultValue>'
			else ''
			end
			+case
			when ltrim(rtrim(G.AgeGroup))!='' then '<DefaultValue><FeedAttributeId>18</FeedAttributeId><Value>'+ltrim(rtrim(G.AgeGroup))+'</Value></DefaultValue>'
			else ''
			end
			+case
			when ltrim(rtrim(G.Color))!='' then '<DefaultValue><FeedAttributeId>19</FeedAttributeId><Value>'+ltrim(rtrim(G.Color))+'</Value></DefaultValue>'
			else ''
			end
			+case
			when ltrim(rtrim(G.Size))!='' then '<DefaultValue><FeedAttributeId>20</FeedAttributeId><Value>'+ltrim(rtrim(G.Size))+'</Value></DefaultValue>'
			else ''
			end
		+'</DefaultValues>' as DefaultValuesXML,0 as TypeActiveId
	from GoogleProduct G
	where (ltrim(rtrim(G.Gender))!=''
		or ltrim(rtrim(G.AgeGroup))!=''
		or ltrim(rtrim(G.Color))!=''
		or ltrim(rtrim(G.Size))!='')
		and G.ProductId not in (select ProductId from FNS_FeedProduct where FeedId=@FeedId)
end
GO
IF not EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[FNS_FeedASIN]') AND name = N'IX_FNS_FeedASIN_ASIN')
	CREATE NONCLUSTERED INDEX IX_FNS_FeedASIN_ASIN ON FNS_FeedASIN ([ASIN] DESC) 
GO
IF not EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[FNS_FeedEPID]') AND name = N'IX_FNS_FeedEPID_EPID')
	CREATE NONCLUSTERED INDEX IX_FNS_FeedEPID_EPID ON FNS_FeedEPID ([EPID] DESC) 
GO
IF not EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Product_SpecificationAttribute_Mapping]') AND name = N'IX_PSAM_ProductId')
	CREATE NONCLUSTERED INDEX [IX_PSAM_ProductId] ON [Product_SpecificationAttribute_Mapping] ([ProductId] ASC)
GO

