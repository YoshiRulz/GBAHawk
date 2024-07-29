{ system ? builtins.currentSystem
, pkgs ? import (builtins.fetchTarball {
	url = "https://github.com/NixOS/nixpkgs/archive/24.05.tar.gz";
	sha256 = "1lr1h35prqkd1mkmzriwlpvxcb34kmhc9dnr48gkm8hh089hifmx";
}) { inherit system; }
, fetchFromGitHub ? pkgs.fetchFromGitHub
}: let
	mainBizHawkRepo = builtins.fetchTarball {
		url = "https://github.com/TASEmulators/BizHawk/archive/7a8b9b13ffdaae4b23edb253bc8a9baf25cc661e.tar.gz";
		sha256 = "0gqk86b48alrd876lq1059ly83j7znkpbq1lw983dp3zw1m91pjc";
	};
	hawkSourceInfo = (hawkAttrs.populateHawkSourceInfo {
		frontendPackageFlavour = "GBAHawk";
		mainAppFilename = "GBAHawk.exe";
		version = "2.9.1";
		src = fetchFromGitHub {
			owner = "alyosha-tas";
			repo = "GBAHawk";
			rev = "0ef5f754e47b38a2850a6ede600477dd388ab8b9";
			postFetch = ''
				cp -t $out/Assets/dll '${mainBizHawkRepo}/Assets/dll/libblip_buf.so'
				touch $out/Assets/GBAHawkMono.sh
				mkdir $out/Assets/Shaders
				mkdir $out/Dist
				cd $out/Dist
				cp -t . '${mainBizHawkRepo}'/Dist/*.sh '${mainBizHawkRepo}'/Dist/.*.sh
				for f in .BuildInConfigX.sh .InvokeCLIOnMainSln.sh; do
					sed 's/BizHawk.sln/GBAHawk.sln/g' -i "$f"
				done
				sed 's/EmuHawk/GBAHawk/g' -i Package.sh
				cat '${builtins.path { path = ./OSTailoredCode.cs; name = "known-good-OSTC"; }}' >$out/src/BizHawk.Common/OSTailoredCode.cs
				sed 's/LinkedLibManager.LoadOrThrow(dllName)/LinkedLibManager.LoadOrThrow(OSTailoredCode.IsUnixHost ? UnixResolveFilePath(dllName) : dllName)/' \
					-i $out/src/BizHawk.Common/IImportResolver.cs
				cat '${builtins.path { path = ./MemoryBlockLinuxPal.cs; name = "known-good-MemBlockLinux"; }}' >$out/src/BizHawk.BizInvoke/MemoryBlockLinuxPal.cs
				sed 's/new MemoryBlockWindowsPal(Size)/OSTailoredCode.IsUnixHost ? new MemoryBlockLinuxPal(Size) : new MemoryBlockWindowsPal(Size)/' \
					-i $out/src/BizHawk.BizInvoke/MemoryBlock.cs
				sed 's/(Func<uint, uint>) Win32Imports.timeBeginPeriod/OSTailoredCode.IsUnixHost ? u => u : Win32Imports.timeBeginPeriod/' \
					-i $out/src/BizHawk.Client.EmuHawk/Throttle.cs
				sed '/_screenBlankTimer.Duration =/i\\t\t\tif (OSTailoredCode.IsUnixHost) return;' \
					-i $out/src/BizHawk.Client.EmuHawk/ScreenSaver.cs
				sed 's/!Config.SkipOutdatedOsCheck/!OSTailoredCode.IsUnixHost \&\& !Config.SkipOutdatedOsCheck/' \
					-i $out/src/BizHawk.Client.EmuHawk/MainForm.cs
				sed '207i\\t\t\t}' -i $out/src/BizHawk.Client.EmuHawk/Program.cs
				sed '206i\\t\t\tif (!OSTC.IsUnixHost) {' -i $out/src/BizHawk.Client.EmuHawk/Program.cs
				sed '/static void CheckLib/i\\t\t\tif (OSTC.IsUnixHost) { AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve; return; } // for Unix, skip everything else and just wire up the event handler' \
					-i $out/src/BizHawk.Client.EmuHawk/Program.cs
			'';
			hash = "sha256-v1omldYQDLIdpcN754afEJ7hbb3fqtY+rBWN/dCg7lM=";
		};
		needsLibGLVND = true;
		nugetDeps = ./deps.nix;
		neededExtraManagedDeps = [
			"flatBuffersCore"
			"flatBuffersGenOutput"
			"gongShell"
			"hawkQuantizer"
			"slimDX"
			"systemDataSqliteDropIn"
		];
	}) // { version = "2.1.3"; };
	hawkAttrs = import mainBizHawkRepo {
		inherit pkgs system;
		doCheck = false;
	};
in hawkAttrs.buildEmuHawkInstallableFor {
	bizhawkAssemblies = (hawkAttrs.buildAssembliesFor hawkSourceInfo).overrideAttrs (oldAttrs: {
		version = "2.1.3+0ef5f754e";
		postPatch = builtins.replaceStrings [ "EmuHawk.csproj" ] [ "GBAHawk.csproj" ] oldAttrs.postPatch;
		installPhase = let
			insertionPoint = "cp -avT Assets $assets";
		in builtins.replaceStrings
			[ "EmuHawk" insertionPoint ]
			[ "GBAHawk" "${insertionPoint}\ntouch $assets/dll/dummy.so; touch $assets/dll/dummy.wbx" ] # this command and a few following it were failing https://github.com/TASEmulators/BizHawk/blob/8e3486a50a6bb5361b5a4ee2419bb701624701e4/Dist/packages.nix#L154
			oldAttrs.installPhase;
	});
}
