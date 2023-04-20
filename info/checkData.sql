use IDCScaleSystem;

select top(100) * from tblScanData 
where Actived = 1
--and BarcodeString ='OPRT8592,6112042102-PAX0-3001,8,6,P,8/8,170000,1/1|1,,,,'
--and IdLabel = '22421.2023'
--and OcNo = 'A100408 '-- AND BoxNo ='1/1'
--and Station = 0
order by CreatedDate desc;

select * from tblCoreDataCodeItemSize 
--where QRLabel ='OPRT8592,6112042102-PAX0-3001,8,6,P,8/8,170000,1/1|1,,,,'
--where IdLabel = '169292.2023'
--Where OC = 'OPRT8594' AND BoxNo ='1/1'
order by CreatedDate desc;

select * from tblCoreDataCodeItemSize
--where CodeItemSize in ( '6812012202-*-3202','6812042201-*-2802')
where CodeItemSize in ( '6818012202-*-3002','6812322201-*-E019','6812012210-*-2302')
order by createddate desc;

select * from tblWinlineProductsInfo
--where CodeItemSize = '6112042001-*-2702'
--where ProductNumber in ('6812042201-PM95-2802','6812012202-2163-3202')
where ProductNumber in ('6812012210-2116-2302','6818012202-2241-3002','6812322201-NBO2-E019')
order by CreatedDate desc

select * from tblMetalScanResult order by createddate desc
SELECT * FROM tblScanDataReject order by createddate desc
 
--delete tblScanData where Id	= 'DE58C27C-E619-4398-B92A-18E9F94814DC'
--delete tblApprovedPrintLabel where Id = 'D88CC4E5-A05F-4BCE-847A-DB31BFEDC31D'

--update tblScanData set ApprovedBy ='00000000-0000-0000-0000-000000000000' where id ='11EE87EC-C546-4DB4-9447-3967246B5629'
--update tblScanData set Status=1 where id ='D29A25A7-3C19-4D92-B3A2-4FE672C744A3'
--update tblScanData set Actived=0 where id ='4532531C-8FE5-4017-BA14-3179773CCFEB'
--update tblCoreDataCodeItemSize set AveWeight1Prs =136 where id='F738E7B3-DA9F-43C3-927A-418552F9EE29'
--update tblWinlineProductsInfo set Decoration=1 where id='026FFF17-02DE-49DC-9EB5-7FB3288907D2'
--update tblScanData set Actived = 0
--F738E7B3-DA9F-43C3-927A-418552F9EE29

--code approved: D7BEF08B-C830-4C67-9F2F-39D52AE178EE