/* A reimplementation of the MsStatusProcess function in FFX-2.
 * This module makes a characters status turns remaining count down
 * on a per character basis, instead of any character's turn decrementing
 * all characters status times.
 * 
 */


[FhLoad(FhGameId.FFX2)]
public unsafe class StatusProcessModule : FhModule {

    protected readonly FhLogger _MsStatusProcess_logger;

    //function delegates and handles
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsStatusProcess();
    private readonly FhMethodHandle<MsStatusProcess>_MsStatusProcess_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);
    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsStatCheckStop(byte chr_id, int param_2);
    private readonly FhMethodHandle<MsStatCheckStop> _MsStatCheckStop_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int F636690(uint chr_id, int param_2, byte param_3);
    private readonly FhMethodHandle<F636690> _FUN_00636690_handle;


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsATBActiveCheck(uint chr_id, uint param_2);
    private readonly FhMethodHandle<MsATBActiveCheck> _MsATBActiveCheck_handle;


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate ushort F6218E0(int param_1);
    private readonly FhMethodHandle<F6218E0> _FUN_006218E0_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int ClampBetween(int param_1, int param_2, int param_3);
    private readonly FhMethodHandle<ClampBetween> _ClampBetween_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void MsStructClear(int* param_1, uint param_2);
    private readonly FhMethodHandle<MsStructClear> _MsStructClear_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsDamageBufferExe(uint chr_id1, uint chr_id2, int param_3);
    private readonly FhMethodHandle<MsDamageBufferExe> _MsDamageBufferExe_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsSetStatus(uint chr_id, uint command_id, int param_3, int param_4);
    private readonly FhMethodHandle<MsSetStatus> _MsSetStatus_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsSetChrWeak(uint chr_id, uint param_2);
    private readonly FhMethodHandle<MsSetChrWeak> _MsSetChrWeak_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsStatusEffectCheck(byte chr_id);
    private readonly FhMethodHandle<MsStatusEffectCheck> _MsStatusEffectCheck_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsMotionRecoverExe(uint chr_id, int param_2);
    private readonly FhMethodHandle<MsMotionRecoverExe> _MsMotionRecoverExe_handle;

    public StatusProcessModule() {
        int addr_offset = 0x400000;

        _MsStatusProcess_logger = new FhLogger("MsStatusProcess_TurnBased.log");
        _MsStatusProcess_handle = new FhMethodHandle<MsStatusProcess>(this, "FFX-2.exe", 0x636eb0 - addr_offset, h_MsStatusProcess);

        _MsStatCheckStop_handle = new FhMethodHandle<MsStatCheckStop>(this, "FFX-2.exe", 0x6430f0 - addr_offset, h_MsStatCheckStop);
        _MsATBActiveCheck_handle = new FhMethodHandle<MsATBActiveCheck>(this, "FFX-2.exe", 0x633f90 - addr_offset, h_MsATBActiveCheck);
        _FUN_006218E0_handle = new FhMethodHandle<F6218E0>(this, "FFX-2.exe", 0x6218E0 - addr_offset, h_F6218E0);//MsCheckStatCount?
        _ClampBetween_handle = new FhMethodHandle<ClampBetween>(this, "FFX-2.exe", 0x624cd0 - addr_offset, h_ClampBetween);
        _FUN_00636690_handle = new FhMethodHandle<F636690>(this, "FFX-2.exe", 0x636690 - addr_offset, h_F636690);
        _MsStructClear_handle = new FhMethodHandle<MsStructClear>(this, "FFX-2.exe", 0x62a0f0 - addr_offset, h_MsStructClear);
        _MsDamageBufferExe_handle = new FhMethodHandle<MsDamageBufferExe>(this, "FFX-2.exe", 0x6422d0 - addr_offset, h_MsDamageBufferExe);
        _MsSetStatus_handle = new FhMethodHandle<MsSetStatus>(this, "FFX-2.exe", 0x636ca0 - addr_offset, h_MsSetStatus);
        _MsSetChrWeak_handle = new FhMethodHandle<MsSetChrWeak>(this, "FFX-2.exe", 0x61b080 - addr_offset, h_MsSetChrWeak);
        _MsStatusEffectCheck_handle = new FhMethodHandle<MsStatusEffectCheck>(this, "FFX-2.exe", 0x623390 - addr_offset, h_MsStatusEffectCheck);
        _MsMotionRecoverExe_handle = new FhMethodHandle<MsMotionRecoverExe>(this, "FFX-2.exe", 0x6330e0 - addr_offset, h_MsMotionRecoverExe);
        _MsGetChr_handle = new FhMethodHandle<MsGetChr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_get_chr_addr);
    }

    
    public int h_get_chr_addr(uint chr_id) {
        return _MsGetChr_handle.orig_fptr.Invoke(chr_id);
    }

    public uint h_MsStatCheckStop(byte chr_id, int param_2) {
        return _MsStatCheckStop_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public byte h_MsATBActiveCheck(uint chr_id, uint param_2) {
        return _MsATBActiveCheck_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public ushort h_F6218E0(int param_1) {
        return _FUN_006218E0_handle.orig_fptr.Invoke(param_1);
    }
    public int h_ClampBetween(int param_1, int param_2, int param_3) {
        return _ClampBetween_handle.orig_fptr.Invoke(param_1, param_2, param_3);
    }
    public int h_F636690(uint chr_id, int param_2, byte param_3) {
        return _FUN_00636690_handle.orig_fptr.Invoke(chr_id, param_2, param_3);
    }
    public unsafe void h_MsStructClear(int* param_1, uint param_2) {
        _MsStructClear_handle.orig_fptr.Invoke(param_1, param_2);
    }
    public void h_MsDamageBufferExe(uint chr_id1, uint chr_id2, int param_3) {
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
    }
    public int h_MsMotionRecoverExe(uint chr_id, int param_2) {
        return _MsMotionRecoverExe_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public unsafe void h_MsStatusProcess() {
        short sVar1;
        int chr_base;
        uint uVar3;
        int iVar4;
        int iVar5;
        uint uVar6;
        uint psn_or_regen_state;
        uint uVar7;
        int *piVar8;
        int psn_or_regen_amount;
        int iVar9;
        bool bVar10;
        uint local_48;
        uint ChrSpeedVal2;
        int local_38;
        uint ChrSpeedVal3;
        short *local_30;
        int local_2c;
        int local_28;
        uint chr_id;
        //byte[] local_1c [4];
        byte[] local_1c = new byte[4];
        uint local_18;
        int local_14;
        uint local_8;
        uint ChrSpeedVal4;


        chr_id = 0;// Replace this line with who has current turn calculation

        chr_base = _MsGetChr_handle.orig_fptr.Invoke(chr_id);
        /* Checks character is active, ?, has HP remaining, some flag ,is not petrified */
        if ((((*(char*)(chr_base + 0x1784) != '\0') && (*(char*)(chr_base + 0x1792) == '\0')) && (0 < *(int*)(chr_base + 0x3b4))) &&
           ((*(char*)(chr_base + 0x1787) == '\0' && ((*(byte*)(chr_base + 0x434) & 2) == 0)))) {
            /* read the character's speed values and store */
            ChrSpeedVal2 = *(uint*)(chr_base + 0x9e8);
            ChrSpeedVal3 = *(uint*)(chr_base + 0x9ec);
            ChrSpeedVal4 = *(uint*)(chr_base + 0x9f0);
            local_2c = 0;
            local_28 = 0;
            local_38 = 0;
            /* Gets a small bitfield that has bits set if character is asleep, petrified or stopped */
            uVar3 = _MsStatCheckStop_handle.orig_fptr.Invoke((byte)chr_id, 0);//6430f0

            /* If under sleep/pet/stop - manipulate speed values 
            if (uVar3 != 0) {
                speedState3 = speedState3 & ~-(uint)((uVar3 & 0xfffffffb) != 0);
                speedState2 = 0;
            }
            *///Replaced this block with next
            if (uVar3 != 0) {
                if ((uVar3 & 0xFFFFFFFB) != 0) {
                    ChrSpeedVal3 = 0;
                }
                ChrSpeedVal2 = 0;
            }

            iVar4 = _MsATBActiveCheck_handle.orig_fptr.Invoke(chr_id, 1);//633f90
            if (iVar4 != 0) {
                /* Duplicate of status bitfield */
                uVar7 = *(uint*)(chr_base + 0x450);
                iVar4 = 0;
                //pointer to Chr off_count array - stores how long before Blind, Silence, Berserk etc expire
                piVar8 = (int*)(chr_base + 0x454);
                uVar3 = 1;
                do {
                    if (((uVar7 >> ((byte)iVar4 & 0x1f) & 1) != 0) && (0 < *piVar8)) {
                        /* DO { Decrement Chr off_count equivalent array
                           } */
                        iVar5 = (int)(*piVar8 - ChrSpeedVal4);
                        if (iVar5 < 0) {
                            local_2c = local_2c + 1;
                            local_28 = local_28 + 1;
                            iVar5 = 0;
                            if ((uVar7 & 4) != 0) {
                                local_38 = local_38 + 1;
                            }
                            uVar7 = uVar7 & ~uVar3;
                        }
                        *piVar8 = iVar5;
                    }
                    //uVar3 = uVar3 << 1 | (uint)((int)uVar3 < 0);
                    uVar3 = (uVar3 << 1) | ((uVar3 & 0x80000000) >> 31); // commented line replaced with this
                    iVar4 = iVar4 + 1;
                    piVar8 = piVar8 + 1;
                } while (iVar4 < 0x18);
                *(uint*)(chr_base + 0x450) = uVar7;
            }
            iVar4 = _MsATBActiveCheck_handle.orig_fptr.Invoke(chr_id, 7);//633f90
            if (iVar4 != 0) {
                local_30 = (short*)(chr_base + 0x4cc);
                iVar4 = 0;
                do {
                    /* Start of 2nd batch of status timers
                       Whichs status time remaining depends on what iVar4 is */
                    iVar5 = (int)*(char*)(chr_base + 0x4b4 + iVar4);
                    /* Decremented value */
                    uVar3 = (uint)iVar5 - 1;
                    if (uVar3 < 0x7d) {
                        uVar7 = _FUN_006218E0_handle.orig_fptr.Invoke(iVar4);
                        iVar9 = 0;
                        if ((uVar7 & 4) != 0) {
                            /* Reads rom.bin count_value (the first one) */
                            //iVar9 = DAT_00df8e84;
                            iVar9 = (int)FhUtil.get_at<uint>(0x9f8e84);
                        }
                        bVar10 = (uVar7 & 8) != 0;
                        if (bVar10) {
                            /* Reads rom.bin count_value (the second one) */
                            //iVar9 = DAT_00df8e88;
                            iVar9 = (int)FhUtil.get_at<uint>(0x9f8e88);
                        }
                        /*
                        local_48 = (uint)bVar10;
                        if (((int)(uint)bVar10 < iVar5) && (iVar9 != 0)) */


                        if (iVar9 != 0 && (!bVar10 || iVar5 > 1)) {
                            uVar6 = ChrSpeedVal3;

                            /*
                            if (((uVar7 & 0x40) == 0) && (uVar6 = speedState2, (char)uVar7 < '\0')) {
                                uVar6 = speedState4;
                            }*/
                            // Replaced previous block with this:
                            if ((uVar7 & 0x40) == 0) {
                                // This assignment MUST happen before the second condition
                                uVar6 = ChrSpeedVal2;

                                if ((uVar7 & 0x80) != 0) {
                                    uVar6 = ChrSpeedVal4;
                                }
                            }



                            //sVar1 = *local_30 + (short)uVar6;
                            sVar1 = (short)(*local_30 + (short)uVar6);

                            *local_30 = sVar1;
                            /* something, and If chr not stopped
                               clamp value properly
                               and update decremented status value */
                            if ((iVar9 <= sVar1) && (uVar6 != 0)) {
                                local_2c = local_2c + 1;
                                *local_30 = (short)(sVar1 - iVar9);

                                local_48 = bVar10 ? 1u : 0u;
                                iVar5 = _ClampBetween_handle.orig_fptr.Invoke((int)uVar3, (int)local_48, 0x7d); //624cd0
                                /* Update decremented status value in Second set of Chr sbyte timed statuses */
                                *(char*)(chr_base + 0x4b4 + iVar4) = (char)iVar5;
                                if (iVar5 <= (int)local_48) {
                                    local_28 = local_28 + 1;
                                    _FUN_00636690_handle.orig_fptr.Invoke(chr_id, chr_base, (byte)uVar7);
                                }
                            }
                        }
                    }
                    local_30 = local_30 + 1;
                    iVar4 = iVar4 + 1;
                } while (iVar4 < 0x18);
                iVar4 = 0;
                do {
                    
                    if (iVar4 == 0) {
                        /* Set to Poison state here */
                        psn_or_regen_state = *(uint*)(chr_base + 0x434) >> 5 & 1;
                    LAB_00637101:
                        /* If poisoned */
                        if (psn_or_regen_state != 0) {
                            /* Get the Poison/regen time accumulator value */
                            piVar8 = (int*)(chr_base + 0x684 + iVar4 * 4);
                            //increase the a
                            *piVar8 = *piVar8 + (int)ChrSpeedVal3;
                            /* Read the accumulator value again */
                            iVar5 = *(int*)(chr_base + 0x684 + iVar4 * 4);
                            /* If th accumulator value is greater than the threshold value */
                            if (*(int*)(chr_base + 0x68c + iVar4 * 4) < iVar5) {
                                psn_or_regen_amount = 0;
                                /* Set the accumulator to be the previously read accumulator value - the threshold value */
                                *(int*)(chr_base + 0x684 + iVar4 * 4) = iVar5 - *(int*)(chr_base + 0x68c + iVar4 * 4);
                                /* If poison */
                                if (iVar4 == 0) {
                                    /* iVarJ is poison damage number multiplied by (chr max_hp / 256) */
                                    psn_or_regen_amount = (*(int*)(chr_base + 0x694) * *(int*)(chr_base + 0x384)) >> 8;
                                }
                                else {
                                    /* If regen */
                                    if (iVar4 == 1) {
                                        /* iVarJ is regen healing value multiplied by (chr max_hp / 256) */
                                        //psn_or_regen_amount = -((uint)(*(int*)(chr_base + 0x698) * *(int*)(chr_base + 0x384)) >> 8);
                                        
                                        //is just regen formula (x/256) * Max HP ----- made negative to heal not damage
                                        uint chr_regen_numerator = *(uint*)(chr_base + 0x698);
                                        int chr_max_hp =  *(int*)(chr_base + 0x384);

                                        psn_or_regen_amount = -(int)(chr_regen_numerator / 256) * chr_max_hp;

                                    }
                                }
                                _MsStructClear_handle.orig_fptr.Invoke(local_1c, 0x14);//62a0f0
                                local_18 = 0x100ff;
                                local_1c[0] = (byte)chr_id;
                                local_14 = psn_or_regen_amount;
                                _MsDamageBufferExe_handle.orig_fptr.Invoke(chr_id, chr_id, local_1c);//6422d0
                                break;
                            }
                        }
                    }
                    else if (iVar4 == 1) {
                        psn_or_regen_state = (uint)*(char*)(chr_base + 0x43b); // is equal to Chr regen timer value
                        goto LAB_00637101;
                    }
                    iVar4 = iVar4 + 1;
                } while (iVar4 < 2);
            }
            if (local_2c != 0) {
                /* Status timer decrementer */
                _MsSetStatus_handle.orig_fptr.Invoke(chr_id, 0xff, 1, 1);//636ca0
                _MsSetChrWeak_handle.orig_fptr.Invoke(chr_id, 0xffffffff);//61b080
            }
            if (local_28 == 0) {
                chr_base = 0;
            }
            else {
                chr_base = 2;
                _MsStatusEffectCheck_handle.orig_fptr.Invoke((byte)chr_id);//623290
            }
            if (local_38 == 0) {
                if (chr_base == 0) goto LAB_006371da;
            }
            else {
                chr_base = 0xc;
            }
            _MsMotionRecoverExe_handle.orig_fptr.Invoke(chr_id, chr_base);//6330e0
        }
    LAB_006371da:
        return;
    }


    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        _MsStatusProcess_handle.hook();
        _MsGetChr_handle.hook();

        _MsStatCheckStop_handle.hook();
        _MsATBActiveCheck_handle.hook();
        _FUN_006218E0_handle.hook();
        _ClampBetween_handle.hook();
        _FUN_00636690_handle.hook();
        _MsStructClear_handle.hook();
        _MsDamageBufferExe_handle.hook();
        _MsSetStatus_handle.hook();
        _MsSetChrWeak_handle.hook();
        _MsStatusEffectCheck_handle.hook();
        _MsMotionRecoverExe_handle.hook();
        return true;
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
