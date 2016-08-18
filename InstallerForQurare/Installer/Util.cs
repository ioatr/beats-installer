using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PacketBeatInstaller
{
    public static class Util
    {

        public static void InstallPacketBeatService(string servicePath)
        {
            ServiceProcessInstaller ProcesServiceInstaller = new ServiceProcessInstaller();
            ProcesServiceInstaller.Account = ServiceAccount.LocalSystem;

            ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
            String __path = String.Format("/assemblypath={0}", servicePath);
            String[] cmdline = { __path };
            ServiceInstallerObj.Context = new InstallContext("", cmdline);
            ServiceInstallerObj.ServiceName = Program.PACKETBEAT_SERVICE_NAME;
            ServiceInstallerObj.DisplayName = Program.PACKETBEAT_SERVICE_NAME;
            ServiceInstallerObj.Description = Program.PACKETBEAT_SERVICE_NAME;
            ServiceInstallerObj.ServiceName = Program.PACKETBEAT_SERVICE_NAME;
            ServiceInstallerObj.StartType = ServiceStartMode.Automatic;
            ServiceInstallerObj.Parent = ProcesServiceInstaller;

            System.Collections.Specialized.ListDictionary state = new System.Collections.Specialized.ListDictionary();
            ServiceInstallerObj.Install(state);
        }

        public static ServiceController FindServiceByName(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            return services.FirstOrDefault(s => string.Compare(s.ServiceName, serviceName, true) == 0);
        }

        public static void UninstallService(string serviceName)
        {
            // 이미등록된 서비스를 제거한다
            ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
            ServiceInstallerObj.Context = new InstallContext();
            ServiceInstallerObj.ServiceName = serviceName;
            ServiceInstallerObj.Uninstall(null);
        }

        /* 실행 시 관리자 권한 상승을 위한 함수 시작 */
        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (null != identity)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);

            }
            return false;
        }

        public static string ReadConfigStringFromResourceFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PacketBeatInstaller.Resources.ConfigTemplate.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

    }
}
