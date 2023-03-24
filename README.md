# 20221201CheckWeightFullAutomationfFT
băng chuyền cân phân loại full automation nhà máy fFT

Hệ thống kết nối 2 PLC, 3 scanner zebra DS DS457 sử dụng SDK, và 1 máy in trực tiếp lên thùng Anser Smart U2
Máy in và plC Delta chỗ bàn cân dùng kết nối COM, sử dụng bộ converter ATC-1000

THÔNG TIN IP/PORT CÁC THIẾT BỊ
1. PC: 192.168.80.101 
2. Convert ATC-1000 PLC: 192.168.80.1/23
3. Convert ATC-1000 Printing: 192.168.80.2/24
4. PLC Conveyor: S7-1200 Profinet: 192.168.80.3

THÔNG TIN ĐỊA CHỈ MAPPING
1. PLC Scale:
    - Bắt đầu từ thanh ghi D500--> địa chỉ tuyệt đối 4596 holding register
    - 4596 (Word): báo sắn sàng.
    - 4597 Double word: giá trị scale Stable
    - 4598 Double word: giá trị scale real time
    - 4599 (Word): bật lên 1 báo cân đã ổn định. khi nào tag này bật lên thì mình sẽ get khối lượng cân về
    - 4600 (Word): Sansor In
    - 4601 (Word): Sensor Out
    - 4602 (Word): Điều khiển đèn tháp. 0--off; 1-Đèn xanh; 2-đèn đỏ
2. PLC Conveyor:
    - Sử dụng vùng nhớ DB1
    - DB1[0]: Sorting conveyor. default 0- metalscan; 1- cant't read barcode (reject); 2- no metal scan. Dưới PLC sau khi nhận tín hiệu thực hiện xong thì tự reset về 0
    - DB1[1]: Pusher scale: khi nào hàng cân fail thì ghi lên 1. Dưới PLC sau khi nhận tín hiệu thực hiện xong thì tự reset về 0
    - DB1[2]: Pusher print ở cuối chuyền, phân loại hàng FG hay là hàng đi sơn. Default =0 hàng FG. Sau khi scan QE code, nếu hàng sơn thì ghi 1. Dưới PLC sau khi nhận tín hiệu thực hiện xong thì tự reset về 0
    - DB1[3]: sensor trước vị trí scan QR metal. sensor này dùng để tính thời gian để báo quét QR code ko đc, khi sensor tác động sẽ bắt đầu tính thời gian, sau khoảng thời gian này mà chưa nhận đc QR code thì sẽ báo là ko đọc đc, và ghi giá trị báo ko quét đc để pusher reject (DB1[0]).
    -DB1[4]: Metal check result
    - DB1[5]: Sensor After metal scan machine
    - DB1[6] MetalPusher
    - DB1[7]: Sensor middle metal. nằm trên sỏtingConveyor
    - DB1[8]: sensor print left(FG)
    - DB1[9]: sensor print right (to Supplier)