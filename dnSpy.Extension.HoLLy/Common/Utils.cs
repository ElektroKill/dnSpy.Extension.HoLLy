using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using dnSpy.Contracts.Debugger;
using HoLLy.dnSpyExtension.Common.CodeInjection;

namespace HoLLy.dnSpyExtension.Common
{
    internal static class Utils
    {
        public static bool IsDebugBuild => IsDebugBuildLazy.Value;

        private static readonly Lazy<bool> IsDebugBuildLazy = new(() => IsAssemblyDebugBuild(typeof(Utils).Assembly));

        public static Guid XorGuid(this Guid g1, Guid g2)
        {
            byte[] bytes1 = g1.ToByteArray();
            byte[] bytes2 = g2.ToByteArray();

            for (var i = 0; i < bytes1.Length; i++) {
                bytes2[i] ^= bytes1[i];
            }

            return new Guid(bytes2);
        }

        public static RuntimeType GetRuntimeType(this DbgRuntime rt) => rt.Name switch {
                "CLR v2.0.50727" => RuntimeType.FrameworkV2,
                "CLR v4.0.30319" => RuntimeType.FrameworkV4,
                "CoreCLR" => RuntimeType.NetCore,
                "Unity" => RuntimeType.Unity,
                _ => throw new ArgumentOutOfRangeException(),
            };

        public static string CopyToTempPath(string path)
        {
            string dir = Path.Combine(Path.GetTempPath(), "dnSpy.Extension.HoLLy");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string newPath = Path.Combine(dir, Guid.NewGuid() + Path.GetExtension(path));
            File.Copy(path, newPath);
            return newPath;
        }

        /// <summary>
        /// Install assembly resolver that looks in the directory of the extension. This is required on .NET Framework,
        /// not on .NET Core.
        /// </summary>
        public static void InstallLocalAssemblyResolver()
        {
            string? extensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? null;

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                if (extensionDirectory is null) return null;

                var asmName = new AssemblyName(args.Name);
                string dllPath = Path.Combine(extensionDirectory, asmName.Name + ".dll");

                if (!File.Exists(dllPath)) return null;

                try
                {
                    return Assembly.LoadFile(dllPath);
                }
                catch
                {
                    return null;
                }
            };
        }

        /// <remarks>https://stackoverflow.com/a/2186634</remarks>
        private static bool IsAssemblyDebugBuild(Assembly assembly)
        {
            return assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        }
    }
}
