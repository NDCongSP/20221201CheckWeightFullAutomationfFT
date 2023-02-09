declare @mesoyear int 
	set @mesoyear = (select MESOYEAR from [FTT2021].[dbo].[vCompanyCurrentYear])

select 
	v21.c002 ProductNumber,
	concat(v21.c011,'-*-',RIGHT(v21.c002,4)) CodeItemSize,
	v21.c003 ProductName,
	v21.c005 ProductCategory,--=1 nó là thằng HeelCounter
	t69.c011  Brand,
	case
		when t70Deco.c001 =1 then 1
		else 0
	end Decoration,
	--t70Deco.c002 Properties,
	v21.c011 MainProductNo,
	v21.c080 MainProductName,
	v21.c074 Color,
	v21.c067 SizeCode,
	(select c002 from [FTT2021].[dbo].t314 where mesoyear = @mesoyear and c003 = v21.c067) SizeName,
	CAST(v21.c063 as float) Weight,

	CAST(v21.c215 as float) LeftWeight,
	CAST(v21.c216 as float) RightWeight,
	v21.c217 BoxType,
	v21.c202 ToolingNo,
	v21.c201 PackingBoxType,
	v21.c203 CustomeUsePb	
from [FTT2021].[dbo].v021 v21 
	left join [FTT2021].[dbo].t070 t70Brand on t70Brand.c000=v21.c010 and t70Brand.c001=0 and t70Brand.mesoyear = @mesoyear --brandName
	left join [FTT2021].[dbo].t070 t70Deco on t70Deco.c000=v21.c010 and t70Deco.c001=1 and t70Deco.mesoyear = @mesoyear --decoration
	left join [CWLSYSTEM].[dbo].T069CMP t69 on t69.c001 = t70Brand.c002
where v21.mesoyear = @mesoyear 
	and (v21.c002 like '6%' or v21.c002 like '7%') 
	and v21.c038  is null 
	and v21.c014 = 2 -- c014 =2  productItem; c014=1  mainItem
order by ProductNumber asc


select * from [FTT2021].[dbo].v021 _v021 where _v021.mesoyear = 1476
select * from [FTT2021].[dbo].t026 _t026 where _t026.mesoyear = 1476--c067 OC