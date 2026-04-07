using static Fahrenheit.Modules.FFX2TurnBased.ATBRecoveryModule; // allows this to use DamageBuffer struct defined in ATBRecovery_StatusHandling.cs

namespace Fahrenheit.Modules.FFX2TurnBased;

public unsafe partial class ATBFillModule : FhModule {

    //function delegates and handles

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
    public unsafe delegate void MsStructClear(void* param_1, uint param_2);
    private readonly FhMethodHandle<MsStructClear> _MsStructClear_handle;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsDamageBufferExe(uint chr_id1, uint chr_id2, void* param_3);
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

    //625160 - MsGetComData
    /* this function returns a base address for a command as far as the scope of ATB recovery time is concerned.
     * In reality, it checks a whole range of things, commands (item, command, monmagic), auto-abilities, Garment Grids*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int MsGetComData(uint command_id, byte* param_2);



    //Hooked functions
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
    }
    public int h_MsMotionRecoverExe(uint chr_id, int param_2) {
        return _MsMotionRecoverExe_handle.orig_fptr.Invoke(chr_id, param_2);
    }

    public unsafe int h_MsGetComData(uint command_id, byte* param_2) {
        return _MsGetComData_handle.orig_fptr.Invoke(command_id, param_2);
    }


    public int cannnotActRestTime(uint chr_id, uint command_id) {
        int chr_base_address;
        int cmd_base_address;

        /* Gets the character's base address */
        chr_base_address = h_MsGetChr(chr_id);
        // FUN_00625160 - Get the commands base address, this function can also return other Excel data types
        cmd_base_address = h_MsGetComData(command_id, (byte*)(0));

        //normal calculation
        //read the commands atb_cost and multiply
        int cmd_recovery_time = (int)(*(ushort*)(cmd_base_address + 0x22) * 10000);

        //divide that by (user's Agility + 1) -- VANILLA
        //uint agility_divisor = (uint)(*(byte*)(chr_base_address + 0x39a)) + 1;

        //CUSTOM DIVISOR
        byte agility = (*(byte*)(chr_base_address + 0x39a));
        double divisor;
        //if agility is over 100, use a stronger taper that means higher agility stats don't reduce recovery time as much.
        if (agility > 100) {
            divisor = Math.Pow(agility + 125, 0.85) + 1;
        }
        else {
            //if agility is 99 or less, use vanilla divisor
            divisor = agility + 1;
        }

        uint agility_divisor = (uint)Math.Round(divisor);



        //delay from attacks to be added
        uint accrued_delay = (uint)*(int*)(chr_base_address + 0x9e0);
        //calculate ATB timer length and clamp between 0 and 99999
        int calced_recovery = h_ClampBetween((int)((cmd_recovery_time / agility_divisor) + accrued_delay), 0, 99999);


        //Haste / Slow Modifier
        //if character is hasted - half recovery time
        if (*(byte*)(chr_base_address + 0x43c) != '\0') {
            calced_recovery = calced_recovery / 2;
        }
        //if character is slowed - double recovery time
        if (*(byte*)(chr_base_address + 0x43d) != '\0') {
            calced_recovery = calced_recovery * 2;
        }


        //auto ability recovery time reduction
        ushort command_used = (*(ushort*)(chr_base_address + 0xf3c));
        int percent_reduction = 0;
        //recov_logger.Info("Command charge time percent reduction is: " + percent_reduction);

        //apply auto ability reduction
        calced_recovery = ((100 - percent_reduction) * calced_recovery) / 100;

        // Accrued delay is reset 
        *(int*)(chr_base_address + 0x9e0) = 0;


        //return calculated recovery time to be written
        return calced_recovery;
    }

    public void TbCantActStatusProcess(uint chr_id) {
        int chr_base;
        uint status_bitfield2;
        int local_38;
        int local_2c;
        int local_28;
        //uint chr_id;
        uint ChrSpeedVal4;
        
            chr_base = h_MsGetChr(chr_id);// Get Chr base address
            if ((((*(byte*)(chr_base + 0x1784) != 0) && (*(byte*)(chr_base + 0x1792) == 0)) && (0 < *(int*)(chr_base + 0x3b4))) &&
           ((*(byte*)(chr_base + 0x1787) == 0 && ((*(byte*)(chr_base + 0x434) & 2) == 0)))) {

            // Handle Stop
            // If Stop timer has time remaining
            if (*(sbyte*)(chr_base + 0x4ba) > 0) {
                *(sbyte*)(chr_base + 0x4ba) = (sbyte)(*(sbyte*)(chr_base + 0x4ba) - 1);
                *(sbyte*)(chr_base + 0x43e) = (sbyte)(*(sbyte*)(chr_base + 0x43e) - 1);
            }
                
                
                // read the character's speed value
                ChrSpeedVal4 = *(uint*)(chr_base + 0x9f0);
                local_2c = 0;
                local_28 = 0;
                local_38 = 0;

                // Read Chrs second copy of status bitfield
                status_bitfield2 = *(uint*)(chr_base + 0x450);

                bool isAsleep = (*(uint*)(chr_base + 0x434) >> 2 & 1) == 1;// check sleep state
                uint sleep_off_count = *(uint*)(chr_base + 0x45c);
                

                if ((isAsleep && (0 < sleep_off_count))) {

                    int new_off_count = (int)(sleep_off_count - ChrSpeedVal4);// compute status time remaining

                    //if status has expired
                    if (new_off_count < 0) {
                        local_2c = local_2c + 1;
                        local_28 = local_28 + 1;
                        //new_off_count = 0;
                        //if sleep
                        if ((status_bitfield2 & 4) != 0) {
                            //increment local_38
                            local_38 = local_38 + 1;
                        // unset bit for sleep here
                        status_bitfield2 &= ~(1u << 2);
                         }
                        
                    }
                    *(uint*)(chr_base + 0x45c) = (uint)new_off_count;// write the updated value
                }

            *(uint*)(chr_base + 0x450) = status_bitfield2;// write the character's updated status bitfield

            //Poison Handling

            int* piVar8;
            int ChrSpeedVal3 = *(int*)(chr_base + 0x9ec);

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
                    // Status timer decrementer?
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


            }

         

    }
}
