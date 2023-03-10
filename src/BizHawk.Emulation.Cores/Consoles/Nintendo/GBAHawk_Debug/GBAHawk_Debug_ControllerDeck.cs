﻿using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBAHawk_Debug
{
	public class GBAHawk_Debug_ControllerDeck
	{
		public GBAHawk_Debug_ControllerDeck(string controller1Name, bool subframe)
		{
			Port1 = ControllerCtors.TryGetValue(controller1Name, out var ctor1)
				? ctor1(1)
				: throw new InvalidOperationException($"Invalid controller type: {controller1Name}");

			Definition = new(Port1.Definition.Name)
			{
				BoolButtons = Port1.Definition.BoolButtons
					.ToList()
			};
			
			foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);

			if (subframe)
			{
				Definition.AddAxis("Input Cycle", 0.RangeTo(70224), 70224);
			}

			Definition.MakeImmutable();
		}

		public ushort ReadPort1(IController c)
		{
			return Port1.Read(c);
		}

		public (ushort X, ushort Y) ReadAcc1(IController c)
			=> Port1.ReadAcc(c);

		public byte ReadSolar1(IController c)
		{
			return Port1.SolarSense(c);
		}

		public ControllerDefinition Definition { get; }

		public void SyncState(Serializer ser)
		{
			Port1.SyncState(ser);
		}

		private readonly IPort Port1;

		private static IReadOnlyDictionary<string, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<string, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<string, Func<int, IPort>>
			{
				[typeof(StandardControls).DisplayName()] = portNum => new StandardControls(portNum),
				[typeof(StandardTilt).DisplayName()] = portNum => new StandardTilt(portNum),
				[typeof(StandardSolar).DisplayName()] = portNum => new StandardSolar(portNum)
			};

		public static string DefaultControllerName => typeof(StandardControls).DisplayName();
	}
}
