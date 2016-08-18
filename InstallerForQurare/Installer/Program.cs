using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PacketBeatInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 파일들은 모두 압축이 풀려있다고 가정하자
                // 관리자권한이 있는지 체크해봐야한다.
                /* 실행 시 관리자 권한 상승을 위한 코드 시작 */
                if (/* Main 아래에 정의된 함수 */IsAdministrator() == false)
                {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = true;
                    procInfo.FileName = Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase);
                    procInfo.WorkingDirectory = Environment.CurrentDirectory;
                    procInfo.Verb = "runas";
                    var p = Process.Start(procInfo);
                }
                else
                {
                    // program files 밑에 packetbeat 폴더를 작성한다
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    path = Path.Combine(path, "PacketBeat");
                    if (Directory.Exists(path) == false)
                    {
                        // 디렉토리를 생성한다
                        Directory.CreateDirectory(path);
                    }

                    // 서비스가 등록되어 있나?
                    const string SERVICE_NAME = "PacketBeat";
                    ServiceController[] services = ServiceController.GetServices();
                    var service = services.FirstOrDefault(s => string.Compare(s.ServiceName, SERVICE_NAME, true) == 0);
                    if (service != null)
                    {
                        // 이미등록된 서비스를 제거한다
                        ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
                        ServiceInstallerObj.Context = new InstallContext();
                        ServiceInstallerObj.ServiceName = service.ServiceName;
                        ServiceInstallerObj.Uninstall(null);
                    }

                    // packetbeat 파일을 카피한다
                    File.Copy(Path.Combine(Environment.CurrentDirectory, "packetbeat.exe"), Path.Combine(path, "packetbeat.exe"), true);

                    // 현재 설정된 
                    var device_index = 0;
                    {
                        ProcessStartInfo procInfo = new ProcessStartInfo();
                        procInfo.CreateNoWindow = false;
                        procInfo.RedirectStandardOutput = true;
                        procInfo.UseShellExecute = false;
                        procInfo.Arguments = "-devices";
                        procInfo.FileName = "packetbeat.exe";
                        procInfo.WorkingDirectory = path;
                        var p = Process.Start(procInfo);
                        p.WaitForExit();

                        List<string> lines = new List<string>();
                        for (var line = p.StandardOutput.ReadLine();
                            string.IsNullOrEmpty(line) == false;
                            line = p.StandardOutput.ReadLine())
                        {
                            lines.Add(line);
                        }

                        if (lines.Count == 0)
                        {
                            throw new Exception("네트워크 디바이스가 하나 이상이어야 합니다");
                        }
                        else if (lines.Count == 1)
                        {
                            // 싱글이면 무조건 0번사용
                            device_index = 0;
                        }
                        else
                        {
                            // 인덱스를 받아온다.
                            lines.ForEach(s => { Console.WriteLine(s); });
                            Console.Write("어느 네트워크 디바이스를 캡쳐하시겠습니까? (default: 0)");
                            var selection = Console.ReadLine();
                            if (!string.IsNullOrEmpty(selection))
                            {
                                device_index = int.Parse(selection);
                            }
                        }
                    }



                    // 선택된 디바이스로 yml 파일을 작성한다
                    string yml_text = @"
interfaces:
  device: _DEVICE_NUMBER_

protocols:
  http:
    ports: [_GAME_PORT_]
    send_request: true
    send_response: true
    
    include_body_for: [""text/html"", ""application/json""]

output:
  logstash:
    hosts: [""_LOGSTASH_""]
";
                    yml_text = yml_text.
                        Replace("_DEVICE_NUMBER_", device_index.ToString()).
                        Replace("_GAME_PORT_", "80").
                        Replace("_LOGSTASH_", "logstash.qurare.atr-api.com:5044");

                    using (var writer = new StreamWriter(Path.Combine(path, "packetbeat.yml")))
                    {
                        // 위에서 편집한 내용을 기록한다
                        writer.Write(yml_text);
                    }

                    // 컨피그가 잘 써졌는지 테스트한다
                    {
                        ProcessStartInfo procInfo = new ProcessStartInfo();
                        procInfo.CreateNoWindow = false;
                        procInfo.RedirectStandardOutput = true;
                        procInfo.UseShellExecute = false;
                        procInfo.Arguments = "-configtest";
                        procInfo.FileName = "packetbeat.exe";
                        procInfo.WorkingDirectory = path;
                        var p = Process.Start(procInfo);
                        p.WaitForExit();

                        var line = p.StandardOutput.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            throw new Exception("컨피그 테스트에 실패하였습니다");
                        }
                    }

                    {
                        ServiceProcessInstaller ProcesServiceInstaller = new ServiceProcessInstaller();
                        ProcesServiceInstaller.Account = ServiceAccount.LocalSystem;

                        ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
                        String __path = String.Format("/assemblypath={0}", Path.Combine(path, "packetbeat.exe")); 
                        String[] cmdline = { __path }; 
                        ServiceInstallerObj.Context = new InstallContext("", cmdline);
                        ServiceInstallerObj.ServiceName = SERVICE_NAME;

                        ServiceInstallerObj.DisplayName = SERVICE_NAME;
                        ServiceInstallerObj.Description = SERVICE_NAME;
                        ServiceInstallerObj.ServiceName = SERVICE_NAME;
                        ServiceInstallerObj.StartType = ServiceStartMode.Automatic;
                        ServiceInstallerObj.Parent = ProcesServiceInstaller;

                        System.Collections.Specialized.ListDictionary state = new System.Collections.Specialized.ListDictionary();
                        ServiceInstallerObj.Install(state); 
                    }

                    // 서비스가 시작상태가 아니라면 시작시킨다
                    services = ServiceController.GetServices();
                    service = services.FirstOrDefault(s => string.Compare(s.ServiceName, SERVICE_NAME, true) == 0);
                    if (service == null)
                    {
                        throw new Exception("서비스가 설치되지 않았습니다");
                    }
                    service.Start();

                    Console.WriteLine("PacketBeat 가 정상적으로 잘 설치되었습니다");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        /* 실행 시 관리자 권한 상승을 위한 함수 시작 */
        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (null != identity)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);

            }
            return false;
        }

    }
}
