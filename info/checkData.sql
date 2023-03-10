select top(100) * from tblScanData 
--where BarcodeString ='OPRT8592,6112042102-PAX0-3001,8,6,P,8/8,170000,1/1|1,,,,'
--where IdLabel = '225619.2023'
--Where OcNo = 'OPRT8594' AND BoxNo ='1/1'
--where Station = 0
order by CreatedDate desc;

select top(100)* from tblApprovedPrintLabel 
--where QRLabel ='OPRT8592,6112042102-PAX0-3001,8,6,P,8/8,170000,1/1|1,,,,'
--where IdLabel = '169292.2023'
--Where OC = 'OPRT8594' AND BoxNo ='1/1'
order by CreatedDate desc;

select * from tblCoreDataCodeItemSize
where CodeItemSize in ( '6812012202-*-3202','6812042201-*-2802')
order by createddate desc;

select * from tblWinlineProductsInfo
--where CodeItemSize = '6112042001-*-2702'
where ProductNumber in ('6812042201-PM95-2802','6812012202-2163-3202')
order by CreatedDate desc

select * from tblMetalScanResult order by createddate desc

--delete tblScanData where Id	= '5DC5F8DC-569E-473E-B62D-766C98721B6C'
--delete tblApprovedPrintLabel where Id = 'D88CC4E5-A05F-4BCE-847A-DB31BFEDC31D'

--update tblScanData set ApprovedBy ='00000000-0000-0000-0000-000000000000' where id ='11EE87EC-C546-4DB4-9447-3967246B5629'
--update tblScanData set Status=1 where id ='D29A25A7-3C19-4D92-B3A2-4FE672C744A3'
--update tblScanData set Actived=0 where id ='AEE58C33-8D55-4ECE-99AC-DB8B1C29E912'
update tblCoreDataCodeItemSize set AveWeight1Prs =129.8 where id='E432B0DF-71D2-4D13-A569-72AF7C51A6AA'
update tblWinlineProductsInfo set Decoration=1 where id='026FFF17-02DE-49DC-9EB5-7FB3288907D2'

--code approved: D7BEF08B-C830-4C67-9F2F-39D52AE178EE

--truncate table tblCoreDataCodeItemSize