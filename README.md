# beats-installer

### 설치 방법
1. 'WinPcap_4_1_3.exe' 으로 WinPcap을 설치합니다.
2. 'PacketBeatInstaller.ini' 설정파일을 필요하다면 자신의 환경에 맞게 수정합니다.
3. 'PacketBeatInstaller.exe' 을 실행합니다.
  1. 캡쳐 할 디바이스를 선택합니다. (외부ip를 잘 확인해주세요)
4. 설치가 제대로 되었는지 확인합니다.
  1. [시작] - [실행]에서 'services.msc'를 입력해 서비스 설정창을 엽니다.
  2. 서비스 설정창에서 'PacketBeat'의 상태가 '실행 중'이라면 정상적으로 설치된 것 입니다.
