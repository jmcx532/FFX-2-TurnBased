// SPDX-License-Identifier: MIT

/* This partial class fits in with ATBRecovery_Main.cs
 * This class re-implements X-2's status handling through stubbing out MsStatusProcess
 * which is called continuously, and it's behaviour is replicated in another function
 * that can be called once at the appropriate time and process statuses
 * 
 */

namespace Fahrenheit.Modules.FFX2TurnBased;
public unsafe partial class ATBRecoveryModule : FhModule {

    // Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsStatusProcess();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsStatCheckStop(byte chr_id, int param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int F636690(uint chr_id, int param_2, byte param_3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsATBActiveCheck(uint chr_id, uint param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate ushort F6218E0(int param_1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int ClampBetween(int param_1, int param_2, int param_3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void MsStructClear(void* param_1, uint param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsDamageBufferExe(uint chr_id1, uint chr_id2, void* param_3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsSetStatus(uint chr_id, uint command_id, int param_3, int param_4);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsSetChrWeak(uint chr_id, uint param_2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsStatusEffectCheck(byte chr_id);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsMotionRecoverExe(uint chr_id, int param_2);


    // Method handles
    private readonly FhMethodHandle<MsStatusProcess>_MsStatusProcess_handle;
    private readonly FhMethodHandle<MsStatCheckStop> _MsStatCheckStop_handle;
    private readonly FhMethodHandle<F636690> _FUN_00636690_handle;
    private readonly FhMethodHandle<MsATBActiveCheck> _MsATBActiveCheck_handle;
    private readonly FhMethodHandle<F6218E0> _FUN_006218E0_handle;
    private readonly FhMethodHandle<ClampBetween> _ClampBetween_handle;
    private readonly FhMethodHandle<MsStructClear> _MsStructClear_handle;
    private readonly FhMethodHandle<MsDamageBufferExe> _MsDamageBufferExe_handle;
    private readonly FhMethodHandle<MsSetStatus> _MsSetStatus_handle;
    private readonly FhMethodHandle<MsSetChrWeak> _MsSetChrWeak_handle;
    private readonly FhMethodHandle<MsStatusEffectCheck> _MsStatusEffectCheck_handle;
    private readonly FhMethodHandle<MsMotionRecoverExe> _MsMotionRecoverExe_handle;


    /* This part is in ATBFRecovery_Main.cs
    public ATBRecoveryModule() { 
    
    }
    */

    // DamageBuffer struct used for Poison/Regen
    [StructLayout(LayoutKind.Explicit, Size = 0x14)]
    public struct DamageBuffer {
        [FieldOffset(0x00)] public int chr_id;
        [FieldOffset(0x04)] public ushort unk1;
        [FieldOffset(0x06)] public ushort unk2;
        [FieldOffset(0x08)] public int damage_amount;
        [FieldOffset(0xC)] public int unk3;
        [FieldOffset(0x10)] public int unk4;
    }


    // Turn-based status handling 
    public void TbStatusProcess(uint chr_id) {
        uint ChrSpeedVal2;
        uint ChrSpeedVal3;
        uint ChrSpeedVal4;
        int local_2c;
        int local_28;
        int local_38;
        uint uVar3;
        uint uVar7;
        int iVar4;
        int* piVar8;
        int iVar5;
        short* local_30;
        int iVar9;
        bool bVar10;
        uint uVar6;
        short sVar1;
        uint local_48;
        /*byte[] local_1c [4]; -- original Ghidra decomp 
         * is DamageBuffer for PSN/RGN
         */

        int chr_base = h_MsGetChr(chr_id);

        //status handling
        // Checks character is active, ?, has HP remaining, some chr state flag and if not petrified
        if ((((*(byte*)(chr_base + 0x1784) != 0) && (*(byte*)(chr_base + 0x1792) == 0)) && (0 < *(int*)(chr_base + 0x3b4))) &&
           ((*(byte*)(chr_base + 0x1787) == 0 && ((*(byte*)(chr_base + 0x434) & 2) == 0)))) {

            // read the character's speed values
            ChrSpeedVal2 = *(uint*)(chr_base + 0x9e8);
            ChrSpeedVal3 = *(uint*)(chr_base + 0x9ec);
            ChrSpeedVal4 = *(uint*)(chr_base + 0x9f0);
            local_2c = 0;
            local_28 = 0;
            local_38 = 0;

            // speed value stuff for statuses
            uVar3 = h_MsStatCheckStop((byte)chr_id, 0);// Gets a small bitfield that has bits set if character is asleep, petrified or stopped
            // If under sleep/pet/stop - manipulate speed values - 0 ATB Fill, conditional anim modifier - sleep doesn't freeze, the other two do
            if (uVar3 != 0) {
                if ((uVar3 & 0xFFFFFFFB) != 0) {
                    ChrSpeedVal3 = 0;
                }
                ChrSpeedVal2 = 0;
            }

        //BITFIELD STATUS HANDLING
            // Read Chrs second copy of status bitfield
            uVar7 = *(uint*)(chr_base + 0x450);
            iVar4 = 0;
            //pointer to Chr off_count array - stores how long before Blind, Silence, Berserk etc expire
            piVar8 = (int*)(chr_base + 0x454);
            uVar3 = 1;

            // Cycle over the Chrs off_count array and decrement their status time remaining - For BITFIELD statuses
            do {
                if (((uVar7 >> ((byte)iVar4 & 0x1f) & 1) != 0) && (0 < *piVar8)) {

                    iVar5 = *piVar8 - (int)ChrSpeedVal4;// compute status time remaining

                    //if status has expired
                    if (iVar5 < 0) {
                        local_2c = local_2c + 1;
                        local_28 = local_28 + 1;
                        //iVar5 = 0;
                        if ((uVar7 & 4) != 0) {
                            local_38 = local_38 + 1;
                        }
                        uVar7 = uVar7 & ~uVar3;
                    }

                    *piVar8 = iVar5;// write the updates value
                }
                //uVar3 = uVar3 << 1 | (uint)((int)uVar3 < 0);
                //uVar3 = (uVar3 << 1) | ((uVar3 & 0x80000000) >> 31); 
                uVar3 = (uVar3 << 1) | (uint)((int)uVar3 < 0 ? 1 : 0);// commented line replaced with this

                iVar4 = iVar4 + 1;// increase iterator
                piVar8 = piVar8 + 1;// increase offset to next status
            } while (iVar4 < 0x18);


            *(uint*)(chr_base + 0x450) = uVar7;// write the character's updated status bitfield

        // Sbyte / Count status timer handling
            local_30 = (short*)(chr_base + 0x4cc);// ???
            iVar4 = 0;// iterator value
            do {
                //start of 2nd set of Chr status timers - sbyte Count statuses - base + incrementer offset to select which status timer
                iVar5 = (int)*(byte*)(chr_base + 0x4b4 + iVar4);// Reads the time remaining value

                uVar3 = (uint)iVar5 - 1;// decremented timer value
                                        //If time remaining value is less than 0x7d (126 or 0x7e is used for Auto-Haste, Auto-xxxxx)
                if (uVar3 < 125) {

                    //Block for statuses that use rom.bin->count_value - ? and Doom
                    uVar7 = h_FUN_006218E0(iVar4);// MsCheckStatCount? -- //return *(undefined2 *)(&DAT_00d49804 + iVar4 * 6);
                    iVar9 = 0;
                    if ((uVar7 & 4) != 0) {
                        /* Reads rom.bin count_value (the first one) */
                        //iVar9 = DAT_00df8e84;
                        iVar9 = (int)FhUtil.get_at<uint>(0x9f8e84);
                    }
                    bVar10 = (uVar7 & 8) != 0;// is true if under some status, but uVar7 changes ^^^ different iterator
                    if (bVar10) {
                        // Reads rom.bin count_value (the second one) -- for Doom?
                        //iVar9 = DAT_00df8e88;
                        iVar9 = (int)FhUtil.get_at<uint>(0x9f8e88);
                    }


                    /*
                    local_48 = (uint)bVar10;
                    if (((int)(uint)bVar10 < iVar5) && (iVar9 != 0)) */
                    //if count_value not 0, ?, or has time remaining

                    if (iVar9 != 0 && iVar5 > (bVar10 ? 1 : 0)) {
                        uVar6 = ChrSpeedVal3;

                        /*
                        if (((uVar7 & 0x40) == 0) && (uVar6 = speedState2, (char)uVar7 < '\0')) {
                            uVar6 = speedState4;
                        }*/
                        // Replaced previous block with this:
                        if ((uVar7 & 0x40) == 0) {
                            uVar6 = ChrSpeedVal2;
                            if ((uVar7 & 0x80) != 0) {
                                uVar6 = ChrSpeedVal4;
                            }
                        }



                        //sVar1 = *local_30 + (short)uVar6;
                        //sVar1 = (short)(*local_30 + (short)uVar6);
                        sVar1 = (short)(*local_30 + (int)uVar6);

                        *local_30 = sVar1;
                        /* something, and If chr not stopped
                           clamp value properly
                           and update decremented status value */
                        if ((iVar9 <= sVar1) && (uVar6 != 0)) {
                            local_2c = local_2c + 1;
                            *local_30 = (short)(sVar1 - iVar9);

                            local_48 = bVar10 ? 1u : 0u; // bVar10 as a number
                            iVar5 = h_ClampBetween((int)uVar3, (int)local_48, 0x7d); //624cd0
                            /* Update decremented status value in Second set of Chr sbyte timed statuses */
                            *(byte*)(chr_base + 0x4b4 + iVar4) = (byte)iVar5;
                            if (iVar5 <= (int)local_48) {
                                local_28 = local_28 + 1;
                                h_FUN_00636690(chr_id, chr_base, (byte)uVar7);
                            }
                        }
                    }
                }
                local_30 = local_30 + 1;// increment ?
                iVar4 = iVar4 + 1;// increment iterator
            } while (iVar4 < 0x18);// xxx_status2 arrays have 0x18 sbytes

            //Poison Handling
            bool isPoisoned = (*(uint*)(chr_base + 0x434) >> 5 & 1) == 1;// check poison state
            if (isPoisoned) {
                piVar8 = (int*)(chr_base + 0x684);// Get pointer to Chr poison accumulator time value
                *piVar8 = *piVar8 + (int)ChrSpeedVal3;//Write or increase the accumulator by the Chrs Speed Value
                int psn_accumulator_val = *(int*)(chr_base + 0x684);// Read the updated accumulator

                // If the accumulator value is greater than the threshold value
                if (*(int*)(chr_base + 0x68c) < psn_accumulator_val) {
                    int psn_damage_amount = 0;
                    // Accumulator value is written as: previously read accumulator value - the threshold value
                    *(int*)(chr_base + 0x684) = psn_accumulator_val - *(int*)(chr_base + 0x68c);

                    //psn_damage_amount = (*(int*)(chr_base + 0x694) * *(int*)(chr_base + 0x384)) >> 8; // Poison Dmg Calc: (X/256) * MaxHP
                    psn_damage_amount = (int)((*(int*)(chr_base + 0x694) / 256.0) * *(int*)(chr_base + 0x384));


                    //byte[] local_1c_psn = new byte[0x14];
                    DamageBuffer psn_buffer = new();
                    DamageBuffer* pBuffer = &psn_buffer;

                    h_MsStructClear(pBuffer, 0x14);//62a0f0
                    //local_18 = 0x100ff;
                    psn_buffer.unk1 = 0xff;
                    psn_buffer.unk2 = 0x01;
                    //local_1c[0] = (byte)chr_id;
                    psn_buffer.chr_id = (byte)chr_id;
                    //local_14 = psn_damage_amount;
                    psn_buffer.damage_amount = psn_damage_amount;

                    h_MsDamageBufferExe(chr_id, chr_id, pBuffer);//6422d0

                    
                }
            }

            if (local_2c != 0) {
                /* Status timer decrementer */
                h_MsSetStatus(chr_id, 0xff, 1, 1);//636ca0
                h_MsSetChrWeak(chr_id, 0xffffffff);//61b080
            }

            int iVar6;
            if (local_28 == 0) {
                iVar6 = 0;
            }
            else {
                iVar6 = 2;
                h_MsStatusEffectCheck((byte)chr_id);//623290


            }
            if (local_38 == 0) {
                if (iVar6 == 0) { return; }
                ;
            }
            else {
                iVar6 = 0xc;
            }
            h_MsMotionRecoverExe(chr_id, iVar6);//6330e0

            return;
        }
    }//TbStatusProcess END

    // Regen to process for either all enemies or all allies at the end of the turn
    public void TbRegenProcess() {
        uint ChrSpeedVal2;
        uint ChrSpeedVal3;
        uint ChrSpeedVal4;
        uint uVar3;
        int* piVar8;

        uint chr_id = 0;
        
        do {
            int chr_base = h_MsGetChr(chr_id);// Get Chr base address
            // Checks character is active, ?, has HP remaining, some chr state flag and if not petrified
            if ((((*(byte*)(chr_base + 0x1784) != 0) && (*(byte*)(chr_base + 0x1792) == 0)) && (0 < *(int*)(chr_base + 0x3b4))) &&
               ((*(byte*)(chr_base + 0x1787) == 0 && ((*(byte*)(chr_base + 0x434) & 2) == 0)))) {

                // read the character's speed values
                ChrSpeedVal2 = *(uint*)(chr_base + 0x9e8);
                ChrSpeedVal3 = *(uint*)(chr_base + 0x9ec);
                ChrSpeedVal4 = *(uint*)(chr_base + 0x9f0);

                //6430f0 
                uVar3 = h_MsStatCheckStop((byte)chr_id, 0);// Gets a small bitfield that has bits set if character is asleep, petrified or stopped

                  // If under sleep/pet/stop - manipulate speed values - 0 ATB Fill, conditional anim modifier - sleep doesn't freeze, the other two do
                if (uVar3 != 0) {
                    if ((uVar3 & 0xFFFFFFFB) != 0) {
                        ChrSpeedVal3 = 0;
                    }
                    ChrSpeedVal2 = 0;
                }

                //Regen Handling
                uint regen_time_left = (uint)*(byte*)(chr_base + 0x43b); // is equal to Chr regen timer value
                if (regen_time_left != 0) {
                    piVar8 = (int*)(chr_base + 0x688);// Get pointer to Chrs Regen time accumulator value
                    *piVar8 = *piVar8 + (int)ChrSpeedVal3;// Write or increase the accumulator by the Chrs Speed Value
                    int regen_accumulator_val = *(int*)(chr_base + 0x688);// Read the updated accumulator

                    // If the accumulator value is greater than the threshold value
                    if (*(int*)(chr_base + 0x690) < regen_accumulator_val) {

                        int regen_amount = 0;

                        // Accumulator value is written: previously read accumulator value - the threshold value
                        *(int*)(chr_base + 0x688) = regen_accumulator_val - *(int*)(chr_base + 0x690);

                        //Regen formula (x/256) * Max HP ----- made negative to heal not damage
                        uint chr_regen_numerator = *(uint*)(chr_base + 0x698);
                        int chr_max_hp =  *(int*)(chr_base + 0x384);

                        regen_amount = -(int)((chr_regen_numerator / 256.0) * chr_max_hp);

                        //create new DamageBuffer and get pointer for next 2 function calls
                        DamageBuffer rgn_buffer = new();
                        DamageBuffer* rBuffer = &rgn_buffer;

                        h_MsStructClear(rBuffer, 0x14);//62a0f0
                                                       //local_18 = 0x100ff;
                        rgn_buffer.unk1 = 0xff;
                        rgn_buffer.unk2 = 0x01;
                        //local_1c[0] = (byte)chr_id;
                        rgn_buffer.chr_id = (byte)chr_id;
                        //local_14 = psn_damage_amount;
                        rgn_buffer.damage_amount = regen_amount;

                        h_MsDamageBufferExe(chr_id, chr_id, rBuffer);//6422d0
                    }
                }

            }

            chr_id = chr_id + 1;
            if (0x1e < chr_id) { return; }
        } while (true);
    }

    // Hooked function handling
    //MsStatusProcess nixed
    public void h_MsStatusProcess() {
        return;
    }
    public uint h_MsStatCheckStop(byte chr_id, int param_2) {
        return _MsStatCheckStop_handle.orig_fptr.Invoke(chr_id, param_2);
    }
    public byte h_MsATBActiveCheck(uint chr_id, uint param_2) {
        return _MsATBActiveCheck_handle.orig_fptr.Invoke(chr_id, param_2);
    }
    public ushort h_FUN_006218E0(int param_1) {
        return _FUN_006218E0_handle.orig_fptr.Invoke(param_1);
    }
    public int h_ClampBetween(int param_1, int param_2, int param_3) {
        return _ClampBetween_handle.orig_fptr.Invoke(param_1, param_2, param_3);
    }
    public int h_FUN_00636690(uint chr_id, int param_2, byte param_3) {
        return _FUN_00636690_handle.orig_fptr.Invoke(chr_id, param_2, param_3);
    }
    public unsafe void h_MsStructClear(void* param_1, uint param_2) {
        _MsStructClear_handle.orig_fptr.Invoke(param_1, param_2);
    }
    public void h_MsDamageBufferExe(uint chr_id1, uint chr_id2, void* param_3) {
        _MsDamageBufferExe_handle.orig_fptr.Invoke(chr_id1, chr_id2, param_3);
    }
    public void h_MsSetStatus(uint chr_id, uint command_id, int param_3, int param_4) {
        _MsSetStatus_handle.orig_fptr.Invoke(chr_id, command_id, param_3, param_4);
    }
    public uint h_MsSetChrWeak(uint chr_id, uint param_2) {
        return _MsSetChrWeak_handle.orig_fptr.Invoke(chr_id, param_2);
    }
    public void h_MsStatusEffectCheck(byte chr_id) {
        _MsStatusEffectCheck_handle.orig_fptr.Invoke(chr_id);
        //_fill_logger.Info("h_MsStatusEffectCheck called");
    }
    public int h_MsMotionRecoverExe(uint chr_id, int param_2) {
        return _MsMotionRecoverExe_handle.orig_fptr.Invoke(chr_id, param_2);
    }


    // init and local_state handled in ATBRecovery_Main.cs


}

