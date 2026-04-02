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



    public void TbSleepStopProcess(byte chr_id) {
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

                bool isAsleep = (*(uint*)(chr_base + 0x434) >> 2 & 1) == 1;// check poison state
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


            }

         

    }
}
