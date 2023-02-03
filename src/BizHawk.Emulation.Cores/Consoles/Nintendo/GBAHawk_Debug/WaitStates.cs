﻿using BizHawk.Emulation.Common;
using System;

/*
	$10000000-$FFFFFFFF    Unmapped
	$0E010000-$0FFFFFFF    Unmapped
	$0E000000-$0E00FFFF    SRAM
	$0C000000-$0DFFFFFF    ROM - Wait State 2
	$0A000000-$0BFFFFFF    ROM - Wait State 1
	$08000000-$09FFFFFF    ROM - Wait State 0
	$07000400-$07FFFFFF    Unmapped
	$07000000-$070003FF    OAM
	$06018000-$06FFFFFF    Unmapped
	$06000000-$06017FFF    VRAM
	$05000400-$05FFFFFF    Unmapped
	$05000000-$050003FF    Palette RAM
	$04XX0800-$04XX0800    Mem Control
	$04000000-$040007FF    I/O Regs
	$03008000-$03FFFFFF    Unmapped
	$03000000-$03007FFF    IWRAM
	$02040000-$02FFFFFF    Unmapped
	$02000000-$0203FFFF    WRAM
	$00004000-$01FFFFFF    Unmapped
	$00000000-$00003FFF    BIOS
*/

namespace BizHawk.Emulation.Cores.Nintendo.GBAHawk_Debug
{
	public partial class GBAHawk_Debug
	{
		public int Wait_State_Access_8(uint addr, bool Seq_Access)
		{
			int wait_ret = 1;
			
			if (addr >= 0x08000000)
			{
				if (addr < 0x0E000000)
				{
					if (addr >= 0x0C000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_2_N; } // ROM 2, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_2_S : ROM_Waits_2_N; } // ROM 2
					}
					else if (addr >= 0x0A000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_1_N; } // ROM 1, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_1_S : ROM_Waits_1_N; } // ROM 1
					}
					else
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_0_N; } // ROM 0, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_0_S : ROM_Waits_0_N; } // ROM 0
					}

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;

					if (pre_Cycle_Glitch)
					{
						// lose 1 cycle if prefetcher is holding the bus
						wait_ret += 1;

						// additionally, the prefetch value is not added to the buffer
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}

					//abandon the prefetcher current fetch
					pre_Fetch_Cnt = 0;
					pre_Seq_Access = false;
					pre_Fetch_Cnt_Inc = 0;

					// if the fetch was in ARM mode, discard the whole thing if only part was fetched
					if (!cpu_Thumb_Mode && ((pre_Buffer_Cnt  & 1) != 0))
					{
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}
				}
				else if ((cart_RAM != null) && (addr < 0x10000000))
				{
					wait_ret += SRAM_Waits; // SRAM

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x05000000) && (addr < 0x08000000))
			{
				if (addr >= 0x07000000)
				{

				}
				else if (addr >= 0x06000000)
				{
					if (ppu_VRAM_Access[ppu_Cycle - 1])
					{
						bool VRAM_Block = true;
						int i = ppu_Cycle - 1;

						while (VRAM_Block)
						{
							if (ppu_VRAM_Access[i])
							{
								wait_ret += 1;
								i += 1;
							}
							else
							{
								VRAM_Block = false;
							}
						}

						// possibly save 1 cycle
						wait_ret -= cpu_Cycle_Save;
					}
				}
				else
				{

				}
			}
			else if ((addr >= 0x02000000) && (addr < 0x03000000))
			{
				wait_ret += WRAM_Waits; //WRAM

				// possibly save 1 cycle
				wait_ret -= cpu_Cycle_Save;
			}

			cpu_Cycle_Save = 0;

			return wait_ret;
		}

		public int Wait_State_Access_16(uint addr, bool Seq_Access)
		{
			int wait_ret = 1;

			if (addr >= 0x08000000)
			{
				if (addr < 0x0E000000)
				{
					if (addr >= 0x0C000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_2_N; } // ROM 2, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_2_S : ROM_Waits_2_N; } // ROM 2
					}
					else if (addr >= 0x0A000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_1_N; } // ROM 1, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_1_S : ROM_Waits_1_N; } // ROM 1
					}
					else
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_0_N; } // ROM 0, Forced Non-Sequential
						else { wait_ret += Seq_Access ? ROM_Waits_0_S : ROM_Waits_0_N; } // ROM 0
					}

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;

					if (pre_Cycle_Glitch)
					{
						// lose 1 cycle if prefetcher is holding the bus
						wait_ret += 1;

						// additionally, the prefetch value is not added to the buffer
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}

					//abandon the prefetcher current fetch
					pre_Fetch_Cnt = 0;
					pre_Seq_Access = false;
					pre_Fetch_Cnt_Inc = 0;

					// if the fetch was in ARM mode, discard the whole thing if only part was fetched
					if (!cpu_Thumb_Mode && ((pre_Buffer_Cnt & 1) != 0))
					{
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}
				}
				else if ((cart_RAM != null) && (addr < 0x10000000))
				{
					wait_ret += SRAM_Waits; // SRAM

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x05000000) && (addr < 0x08000000))
			{
				if (addr >= 0x07000000)
				{

				}
				else if (addr >= 0x06000000)
				{
					if (ppu_VRAM_Access[ppu_Cycle - 1])
					{
						bool VRAM_Block = true;
						int i = ppu_Cycle - 1;

						while (VRAM_Block)
						{
							if (ppu_VRAM_Access[i])
							{
								wait_ret += 1;
								i += 1;
							}
							else
							{
								VRAM_Block = false;
							}
						}

						// possibly save 1 cycle
						wait_ret -= cpu_Cycle_Save;
					}
				}
				else
				{

				}
			}
			else if ((addr >= 0x02000000) && (addr < 0x03000000))
			{
				wait_ret += WRAM_Waits; //WRAM

				// possibly save 1 cycle
				wait_ret -= cpu_Cycle_Save;
			}

			cpu_Cycle_Save = 0;

			return wait_ret;
		}

		public int Wait_State_Access_32(uint addr, bool Seq_Access)
		{
			int wait_ret = 1;

			if (addr >= 0x08000000)
			{
				if (addr < 0x0E000000)
				{
					if (addr >= 0x0C000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_2_N + ROM_Waits_2_S + 1; } // ROM 2, Forced Non-Sequential (2 accesses)
						else { wait_ret += Seq_Access ? ROM_Waits_2_S * 2 + 1 : ROM_Waits_2_N + ROM_Waits_2_S + 1; } // ROM 2 (2 accesses)
					}
					else if (addr >= 0x0A000000)
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_1_N + ROM_Waits_1_S + 1; } // ROM 1, Forced Non-Sequential (2 accesses)
						else { wait_ret += Seq_Access ? ROM_Waits_1_S * 2 + 1 : ROM_Waits_1_N + ROM_Waits_1_S + 1; } // ROM 1 (2 accesses)
					}
					else
					{
						if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_0_N + ROM_Waits_0_S + 1; } // ROM 0, Forced Non-Sequential (2 accesses)
						else { wait_ret += Seq_Access ? ROM_Waits_0_S * 2 + 1 : ROM_Waits_0_N + ROM_Waits_0_S + 1; } // ROM 0 (2 accesses)
					}

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;

					if (pre_Cycle_Glitch)
					{
						// lose 1 cycle if prefetcher is holding the bus
						wait_ret += 1;

						// additionally, the prefetch value is not added to the buffer
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}

					//abandon the prefetcher current fetch
					pre_Fetch_Cnt = 0;
					pre_Seq_Access = false;
					pre_Fetch_Cnt_Inc = 0;

					// if the fetch was in ARM mode, discard the whole thing if only part was fetched
					if (!cpu_Thumb_Mode && ((pre_Buffer_Cnt & 1) != 0))
					{
						pre_Buffer_Cnt -= 1;
						pre_Read_Addr -= 2;
					}
				}
				else if ((cart_RAM != null) && (addr < 0x10000000))
				{
					wait_ret += SRAM_Waits; // SRAM

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x05000000) && (addr < 0x08000000))
			{
				if (addr >= 0x07000000)
				{

				}
				else if (addr >= 0x06000000)
				{			
					wait_ret += 1; // PALRAM and VRAM take 2 cycles on 32 bit accesses

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;

					if (ppu_VRAM_Access[ppu_Cycle - 1])
					{
						bool VRAM_Block = true;
						int i = ppu_Cycle - 1;

						while (VRAM_Block)
						{
							if (ppu_VRAM_Access[i])
							{
								wait_ret += 1;
								i += 1;
							}
							else
							{
								VRAM_Block = false;
							}
						}
					}

					// check both edges of the access
					if (ppu_Cycle < 1230)
					{
						if (ppu_VRAM_Access[ppu_Cycle])
						{
							bool VRAM_Block = true;
							int i = ppu_Cycle;

							while (VRAM_Block)
							{
								if (ppu_VRAM_Access[i])
								{
									wait_ret += 1;
									i += 1;
								}
								else
								{
									VRAM_Block = false;
								}
							}
						}
					}
				}
				else
				{
					wait_ret += 1; // PALRAM and VRAM take 2 cycles on 32 bit accesses

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x02000000) && (addr < 0x03000000))
			{
				wait_ret += (WRAM_Waits * 2 + 1); // WRAM (2 accesses)

				// possibly save 1 cycle
				wait_ret -= cpu_Cycle_Save;
			}

			cpu_Cycle_Save = 0;

			return wait_ret;
		}

		public int Wait_State_Access_16_Instr(uint addr, bool Seq_Access)
		{
			int wait_ret = 1;
			pre_Seq_Access = true;

			if (addr >= 0x08000000)
			{
				if (addr < 0x0E000000)
				{
					if (addr == pre_Check_Addr)
					{
						if (pre_Check_Addr != pre_Read_Addr)
						{
							// we have this address, can immediately read it
							pre_Check_Addr += 2;
							pre_Buffer_Cnt -= 1;
						}
						else
						{
							// we are in the middle of a prefetch access, it takes however many cycles remain to fetch it
							// plus 1 since the prefetcher already used this cycle, so don't double count
							wait_ret = pre_Fetch_Wait - pre_Fetch_Cnt + 1;

							pre_Read_Addr += 2;
							pre_Check_Addr += 2;
							pre_Fetch_Cnt = 0;

							// it is as if the cpu takes over a regular access, so reset the pre-fetcher
							pre_Fetch_Cnt_Inc = 0;
						}
					}
					else
					{
						// the address is not related to the current ones available to the prefetcher
						if (addr >= 0x0C000000)
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_2_N; } // ROM 2, Forced Non-Sequential
							else { wait_ret += Seq_Access ? ROM_Waits_2_S : ROM_Waits_2_N; } // ROM 2
						}
						else if (addr >= 0x0A000000)
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_1_N; } // ROM 1, Forced Non-Sequential
							else { wait_ret += Seq_Access ? ROM_Waits_1_S : ROM_Waits_1_N; } // ROM 1
						}
						else
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_0_N; } // ROM 0, Forced Non-Sequential
							else { wait_ret += Seq_Access ? ROM_Waits_0_S : ROM_Waits_0_N; } // ROM 0
						}

						// possibly save 1 cycle
						wait_ret -= cpu_Cycle_Save;

						//abandon the prefetcher current fetch and reset the address
						pre_Buffer_Cnt = 0;
						pre_Fetch_Cnt = 0;
						pre_Fetch_Cnt_Inc = 0;

						if (pre_Enable) { pre_Check_Addr = pre_Read_Addr = addr + 2; }			
					}
				}
				else if ((cart_RAM != null) && (addr < 0x10000000))
				{
					wait_ret += SRAM_Waits; // SRAM

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x05000000) && (addr < 0x08000000))
			{
				if (addr >= 0x07000000)
				{

				}
				else if (addr >= 0x06000000)
				{
					if (ppu_VRAM_Access[ppu_Cycle - 1])
					{
						bool VRAM_Block = true;
						int i = ppu_Cycle - 1;

						while (VRAM_Block)
						{
							if (ppu_VRAM_Access[i])
							{
								wait_ret += 1;
								i += 1;
							}
							else
							{
								VRAM_Block = false;
							}
						}

						// possibly save 1 cycle
						wait_ret -= cpu_Cycle_Save;
					}
				}
				else
				{

				}
			}
			else if ((addr >= 0x02000000) && (addr < 0x03000000))
			{
				wait_ret += WRAM_Waits; //WRAM

				// possibly save 1 cycle
				wait_ret -= cpu_Cycle_Save;
			}

			cpu_Cycle_Save = 0;

			return wait_ret;
		}

		public int Wait_State_Access_32_Instr(uint addr, bool Seq_Access)
		{
			int wait_ret = 1;
			pre_Seq_Access = true;

			if (addr >= 0x08000000)
			{
				if (addr < 0x0E000000)
				{
					if (addr == pre_Check_Addr)
					{
						if (pre_Check_Addr != pre_Read_Addr)
						{
							// we have this address, can immediately read it
							pre_Check_Addr += 2;
							pre_Buffer_Cnt -= 1;

							// check if we have the next address as well
							if (pre_Check_Addr != pre_Read_Addr)
							{
								// the prefetcher can retun 32 bits in 1 cycle if it has it available
								pre_Check_Addr += 2;
								pre_Buffer_Cnt -= 1;
							}
							else
							{
								// we are in the middle of a prefetch access, it takes however many cycles remain to fetch it
								// plus 1 since the prefetcher already used this cycle, so don't double count
								wait_ret = pre_Fetch_Wait - pre_Fetch_Cnt + 1;

								// it is as if the cpu takes over a regular access, so reset the pre-fetcher
								pre_Fetch_Cnt_Inc = 0;

								pre_Read_Addr += 2;
								pre_Check_Addr += 2;
								pre_Fetch_Cnt = 0;
								pre_Buffer_Cnt = 0;
							}
						}
						else
						{
							// we are in the middle of a prefetch access, it takes however many cycles remain to fetch it
							// plus 1 since the prefetcher already used this cycle, so don't double count
							wait_ret = pre_Fetch_Wait - pre_Fetch_Cnt + 1;

							// then add the second access
							if (addr > 0x0C000000)
							{
								wait_ret += ROM_Waits_2_S + 1; // ROM 2
							}
							else if (addr > 0x0A000000)
							{
								wait_ret += ROM_Waits_1_S + 1; // ROM 1
							}
							else
							{
								wait_ret += ROM_Waits_0_S + 1; // ROM 0
							}

							// it is as if the cpu takes over a regular access, so reset the pre-fetcher
							pre_Fetch_Cnt_Inc = 0;

							pre_Read_Addr += 4;
							pre_Check_Addr += 4;
							pre_Fetch_Cnt = 0;
						}
					}
					else
					{
						// the address is not related to the current ones available to the prefetcher
						if (addr >= 0x0C000000)
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_2_N + ROM_Waits_2_S + 1; } // ROM 2, Forced Non-Sequential (2 accesses)
							else { wait_ret += Seq_Access ? ROM_Waits_2_S * 2 + 1 : ROM_Waits_2_N + ROM_Waits_2_S + 1; } // ROM 2 (2 accesses)
						}
						else if (addr >= 0x0A000000)
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_1_N + ROM_Waits_1_S + 1; } // ROM 1, Forced Non-Sequential (2 accesses)
							else { wait_ret += Seq_Access ? ROM_Waits_1_S * 2 + 1 : ROM_Waits_1_N + ROM_Waits_1_S + 1; } // ROM 1 (2 accesses)
						}
						else
						{
							if ((addr & 0x1FFFF) == 0) { wait_ret += ROM_Waits_0_N + ROM_Waits_0_S + 1; } // ROM 0, Forced Non-Sequential (2 accesses)
							else { wait_ret += Seq_Access ? ROM_Waits_0_S * 2 + 1 : ROM_Waits_0_N + ROM_Waits_0_S + 1; } // ROM 0 (2 accesses)
						}

						// possibly save 1 cycle
						wait_ret -= cpu_Cycle_Save;

						//abandon the prefetcher current fetch and reset the address
						pre_Buffer_Cnt = 0;
						pre_Fetch_Cnt = 0;
						pre_Fetch_Cnt_Inc = 0;

						if (pre_Enable) { pre_Check_Addr = pre_Read_Addr = addr + 4; }
					}
				}
				else if ((cart_RAM != null) && (addr < 0x10000000))
				{
					wait_ret += SRAM_Waits; // SRAM

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x05000000) && (addr < 0x08000000))
			{
				if (addr >= 0x07000000)
				{

				}
				else if (addr >= 0x06000000)
				{
					wait_ret += 1; // PALRAM and VRAM take 2 cycles on 32 bit accesses

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;

					if (ppu_VRAM_Access[ppu_Cycle - 1])
					{
						bool VRAM_Block = true;
						int i = ppu_Cycle - 1;

						while (VRAM_Block)
						{
							if (ppu_VRAM_Access[i])
							{
								wait_ret += 1;
								i += 1;
							}
							else
							{
								VRAM_Block = false;
							}
						}
					}

					// check both edges of the access
					if (ppu_Cycle < 1230)
					{
						if (ppu_VRAM_Access[ppu_Cycle])
						{
							bool VRAM_Block = true;
							int i = ppu_Cycle;

							while (VRAM_Block)
							{
								if (ppu_VRAM_Access[i])
								{
									wait_ret += 1;
									i += 1;
								}
								else
								{
									VRAM_Block = false;
								}
							}
						}
					}
				}
				else
				{
					wait_ret += 1; // PALRAM and VRAM take 2 cycles on 32 bit accesses

					// possibly save 1 cycle
					wait_ret -= cpu_Cycle_Save;
				}
			}
			else if ((addr >= 0x02000000) && (addr < 0x03000000))
			{
				wait_ret += (WRAM_Waits * 2 + 1); // WRAM (2 accesses)

				// possibly save 1 cycle
				wait_ret -= cpu_Cycle_Save;
			}

			cpu_Cycle_Save = 0;

			return wait_ret;
		}
	}
}