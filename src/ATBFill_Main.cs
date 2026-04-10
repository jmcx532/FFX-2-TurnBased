// SPDX-License-Identifier: MIT

/* This module replaces vanilla ATB fill behaviour with a turn based approach:
 * calculating the lowest atb_remaining value between applicable characters and
 * deduct that value from everybodies atb_remaining.
 * 
 * Pairs up with ATBFill_StatusHandling.cs which has special handling
 * for statuses that prevent character's from acting.
 */

namespace Fahrenheit.Modules.FFX2TurnBased;

[FhLoad(FhGameId.FFX2)]
public unsafe partial class ATBFillModule : FhModule {
    //offset address
    int addr_offset = 0x400000;

    protected readonly FhLogger _fill_logger;


    //delegates
    //6343d0 - MsChrATBprocess
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsChrATBprocess();
    ////625bf0 - MsGetRamChrMonster
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsGetRamChrMonster(uint chr_id);

    //634a20 - non-existent in Switch ver - Alters Chr ATB Speed values: Haste and Slow, Sleep/Stop/Stone, on-hit effect
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint FUN_00634A20(uint chr_id, int chr_base_addr, uint cfg_atb_speed);
    //644bb0 - MsMagicCheckCommandExe
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int MsMagicCheckCommandExe(int* param_1, uint param_2, int* param_3, int* param_4);
    //60ff90
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsBtlChrNumCheck(byte chr_id);
    //635300
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsActionRequest(uint chr_id, int param_2, int param_3, int param_4);
    //75d0c0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TOBtlSetATBChr(uint chr_id);//this may be missing a param_2
    //648520
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsActionAI(uint chr_id);//this may be missing param_2 and param_3
    //649380
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsAutoConfuseProcess(uint chr_id);//this may be missing a param_2
    //649100
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsAutoBerserkProcess(uint chr_id, int chr_base_address);


    //634b40 - msChrATBprocess
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int msChrATBprocess(byte chr_id, int chr_base_address, int* param_3, int param_4);

    /*634b00 - this function checks some character values including their ATB tick down speed
    *pseudo checks for Stop (i.e their tick down value is 0 but Sleep/Petrify/Stop are better covered
    in the ChrAtbSpeedHandler*/
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int bravo_fx(int param_1);

    //61c290 - MsCheckMonsterOversoul
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckMonsterOversoul(uint param_1);

    //636900 -  Warriors Sentinel related - MsResetDefenseStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsResetDefenseStatus(byte param_1);
    //636400
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsClearDanceStatusMotion(byte param_1);
    //636360 - MsCheckDanceStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckDanceStatus(byte param_1);

    //611450 - MsGetChr
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);

    //handles
    private readonly FhMethodHandle<MsChrATBprocess>_MsChrATBprocess_handle;//6343d0
    private readonly FhMethodHandle<msChrATBprocess>_msChrATBprocess_handle;//634b40
    private readonly FhMethodHandle<FUN_00634A20>_FUN_00634A20_handle;//634b40
    private readonly FhMethodHandle<MsMagicCheckCommandExe> _MsMagicCheckCommandExe_handle;//644bb0
    private readonly FhMethodHandle<MsBtlChrNumCheck> _MsBtlChrNumCheck_handle;//60ff90
    private readonly FhMethodHandle<MsActionRequest> _MsActionRequest_handle;//635300
    private readonly FhMethodHandle<TOBtlSetATBChr> _TOBtlSetATBChr_handle;//75d0c0
    private readonly FhMethodHandle<MsActionAI> _MsActionAI_handle;//648520
    private readonly FhMethodHandle<MsAutoConfuseProcess> _MsAutoConfuseProcess_handle;//649380
    private readonly FhMethodHandle<MsAutoBerserkProcess> _MsAutoBerserkProcess_handle;//649100
    private readonly FhMethodHandle<MsGetRamChrMonster> _MsGetRamChrMonster_handle;//625bf0

    private readonly FhMethodHandle<bravo_fx> _bravo_fx_handle;
    private readonly FhMethodHandle<MsCheckMonsterOversoul> _MsCheckMonsterOversoul_handle;
    private readonly FhMethodHandle<MsResetDefenseStatus> _MsResetDefenseStatus_handle;
    private readonly FhMethodHandle<MsClearDanceStatusMotion> _MsClearDanceStatusMotion_handle;
    private readonly FhMethodHandle<MsCheckDanceStatus> _MsCheckDanceStatus_handle;

    private readonly FhMethodHandle<MsGetChr> _MsGetChr_handle;//611450
    private readonly FhMethodHandle<MsGetComData> _MsGetComData_handle;

    public ATBFillModule() {

        _fill_logger = new FhLogger("TurnBased_FillHandler.log");

        _MsChrATBprocess_handle = new FhMethodHandle<MsChrATBprocess>(this, "FFX-2.exe", 0x6343d0 - addr_offset, h_MsChrATBprocess);
        _FUN_00634A20_handle = new FhMethodHandle<FUN_00634A20>(this, "FFX-2.exe", 0x634a20 - addr_offset, h_FUN_00634A20);
        _MsMagicCheckCommandExe_handle = new FhMethodHandle<MsMagicCheckCommandExe>(this, "FFX-2.exe", 0x644bb0 - addr_offset, h_MsMagicCheckCommandExe);
        _MsBtlChrNumCheck_handle = new FhMethodHandle<MsBtlChrNumCheck>(this, "FFX-2.exe", 0x60ff90 - addr_offset, h_MsBtlChrNumCheck);
        _MsActionRequest_handle = new FhMethodHandle<MsActionRequest>(this, "FFX-2.exe", 0x635300 - addr_offset, h_MsActionRequest);
        _TOBtlSetATBChr_handle = new FhMethodHandle<TOBtlSetATBChr>(this, "FFX-2.exe", 0x75d0c0 - addr_offset, h_TOBtlSetATBChr);
        _MsActionAI_handle = new FhMethodHandle<MsActionAI>(this, "FFX-2.exe", 0x648520 - addr_offset, h_MsActionAI);
        _MsAutoConfuseProcess_handle = new FhMethodHandle<MsAutoConfuseProcess>(this, "FFX-2.exe", 0x649380 - addr_offset, h_MsAutoConfuseProcess);
        _MsAutoBerserkProcess_handle = new FhMethodHandle<MsAutoBerserkProcess>(this, "FFX-2.exe", 0x649100 - addr_offset, h_MsAutoBerserkProcess);
        _MsGetRamChrMonster_handle = new FhMethodHandle<MsGetRamChrMonster>(this, "FFX-2.exe", 0x625bf0 - addr_offset, h_MsGetRamChrMonster);

        _msChrATBprocess_handle = new FhMethodHandle<msChrATBprocess>(this, "FFX-2.exe", 0x634b40 - addr_offset, h_msChrATBprocess);
        _bravo_fx_handle = new FhMethodHandle<bravo_fx>(this, "FFX-2.exe", 0x634b00 - addr_offset, h_bravo_fx);
        _MsCheckMonsterOversoul_handle = new FhMethodHandle<MsCheckMonsterOversoul>(this, "FFX-2.exe", 0x61c290 - addr_offset, h_MsCheckMonsterOversoul);
        _MsResetDefenseStatus_handle = new FhMethodHandle<MsResetDefenseStatus>(this, "FFX-2.exe", 0x636900 - addr_offset, h_MsResetDefenseStatus);
        _MsClearDanceStatusMotion_handle = new FhMethodHandle<MsClearDanceStatusMotion>(this, "FFX-2.exe", 0x636400 - addr_offset, h_MsClearDanceStatusMotion);
        _MsCheckDanceStatus_handle = new FhMethodHandle<MsCheckDanceStatus>(this, "FFX-2.exe", 0x636360 - addr_offset, h_MsCheckDanceStatus);

        _MsGetChr_handle = new FhMethodHandle<MsGetChr>(this, "FFX-2.exe", 0x611450 - addr_offset, h_MsGetChr);
        _MsGetComData_handle = new FhMethodHandle<MsGetComData>(this, "FFX-2.exe", 0x625160 - addr_offset, h_MsGetComData);

        //Status Handles
        _MsStatCheckStop_handle = new FhMethodHandle<MsStatCheckStop>(this, "FFX-2.exe", 0x6430f0 - addr_offset, h_MsStatCheckStop);
        _MsATBActiveCheck_handle = new FhMethodHandle<MsATBActiveCheck>(this, "FFX-2.exe", 0x633f90 - addr_offset, h_MsATBActiveCheck);
        _FUN_006218E0_handle = new FhMethodHandle<F6218E0>(this, "FFX-2.exe", 0x6218E0 - addr_offset, h_FUN_006218E0);//MsCheckStatCount?
        _ClampBetween_handle = new FhMethodHandle<ClampBetween>(this, "FFX-2.exe", 0x624cd0 - addr_offset, h_ClampBetween);//MsCheckRange
        _FUN_00636690_handle = new FhMethodHandle<F636690>(this, "FFX-2.exe", 0x636690 - addr_offset, h_FUN_00636690);
        _MsStructClear_handle = new FhMethodHandle<MsStructClear>(this, "FFX-2.exe", 0x62a0f0 - addr_offset, h_MsStructClear);
        _MsDamageBufferExe_handle = new FhMethodHandle<MsDamageBufferExe>(this, "FFX-2.exe", 0x6422d0 - addr_offset, h_MsDamageBufferExe);
        _MsSetStatus_handle = new FhMethodHandle<MsSetStatus>(this, "FFX-2.exe", 0x636ca0 - addr_offset, h_MsSetStatus);
        _MsSetChrWeak_handle = new FhMethodHandle<MsSetChrWeak>(this, "FFX-2.exe", 0x61b080 - addr_offset, h_MsSetChrWeak);
        _MsStatusEffectCheck_handle = new FhMethodHandle<MsStatusEffectCheck>(this, "FFX-2.exe", 0x623290 - addr_offset, h_MsStatusEffectCheck);
        _MsMotionRecoverExe_handle = new FhMethodHandle<MsMotionRecoverExe>(this, "FFX-2.exe", 0x6330e0 - addr_offset, h_MsMotionRecoverExe);

    }

    public int h_MsGetChr(uint param_1) {
        return _MsGetChr_handle.orig_fptr.Invoke(param_1);
    }
    public unsafe int h_MsMagicCheckCommandExe(int* param_1, uint param_2, int* param_3, int* param_4) {
        return _MsMagicCheckCommandExe_handle.orig_fptr.Invoke(param_1, param_2, param_3, param_4);
    }
    public int h_MsBtlChrNumCheck(byte chr_id) {
        return _MsBtlChrNumCheck_handle.orig_fptr.Invoke(chr_id);
    }
    public int h_MsActionRequest(uint chr_id, int param_2, int param_3, int param_4) {
        return _MsActionRequest_handle.orig_fptr.Invoke(chr_id, param_2, param_3, param_4);
    }
    public void h_TOBtlSetATBChr(uint chr_id) {
        _TOBtlSetATBChr_handle.orig_fptr.Invoke(chr_id);
    }
    public byte h_MsActionAI(uint chr_id) {
        return _MsActionAI_handle.orig_fptr.Invoke(chr_id);
    }
    public byte h_MsAutoConfuseProcess(uint chr_id) {
        return _MsAutoConfuseProcess_handle.orig_fptr.Invoke(chr_id);
    }
    public uint h_MsAutoBerserkProcess(uint chr_id, int chr_base_address) {
        return _MsAutoBerserkProcess_handle.orig_fptr.Invoke(chr_id, chr_base_address);
    }
    public uint h_MsGetRamChrMonster(uint chr_id) {
        return _MsGetRamChrMonster_handle.orig_fptr.Invoke(chr_id);
    }
    public int h_bravo_fx(int param_1) {
        return _bravo_fx_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsCheckMonsterOversoul(uint param_1) {
        return _MsCheckMonsterOversoul_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsResetDefenseStatus(byte param_1) {
        return _MsResetDefenseStatus_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsClearDanceStatusMotion(byte param_1) {
        return _MsClearDanceStatusMotion_handle.orig_fptr.Invoke(param_1);
    }
    public int h_MsCheckDanceStatus(byte param_1) {
        return _MsCheckDanceStatus_handle.orig_fptr.Invoke(param_1);
    }

    // vanilla implementation ready for changes if required -- NOT HOOKED, uncomment in init() if required.
    public void h_MsChrATBprocess() {
        uint uVar1;
        byte cVar2;
        bool bVar3;
        byte uVar4;
        int iVar5;
        int isEnemy;
        int uVar7;
        uint uVar8;
        char cVar9;
        int iVar10;
        int local_90;
        int local_8c;
        int local_88;
        //uint local_84 [31];
        //uint[] local_84 = new uint[31];
        uint* local_84 = stackalloc uint[31];
        //uint local_8;

        iVar10 = 0;
        uVar8 = 0;
        do {

            iVar5 = h_MsGetChr(uVar8);
            isEnemy = (int)h_MsGetRamChrMonster(uVar8);

            if (isEnemy == 1) {
                byte DAT_00df78bb = FhUtil.get_at<byte>(0x9f78bb);
                if (DAT_00df78bb != 0 || *(byte*)(iVar5 + 0x1789) == 0) {
                    iVar5 = h_MsGetChr(uVar8 & 0xff);
                    *(int*)(iVar5 + 0x9e4) = 0;
                    *(int*)(iVar5 + 0x9e8) = 0;
                    *(int*)(iVar5 + 0x9ec) = 0;
                    *(int*)(iVar5 + 0x9f0) = 0;
                    *(int*)(iVar5 + 0x9fc) = 0;
                }
                else {
                    uint DAT_00df8818 = FhUtil.get_at<uint>(0x9f8818);
                    uint DAT_00df881c = FhUtil.get_at<uint>(0x9f881c);
                    // Sets chr speed values depending on haste/slow, applies on-hit slow effect, DF8818 is the ATB speed pulled from the config setting
                    h_FUN_00634A20(uVar8, iVar5, DAT_00df8818);
                    *(int*)(iVar5 + 0x9fc) = (int)DAT_00df881c;
                }
            }
            if (isEnemy == 0) {
                byte DAT_00df78ba = FhUtil.get_at<byte>(0x9f78ba);
                if (DAT_00df78ba != 0 || *(byte*)(iVar5 + 0x1789) == 0) {
                    iVar5 = h_MsGetChr(uVar8 & 0xff);
                    *(int*)(iVar5 + 0x9e4) = 0;
                    *(int*)(iVar5 + 0x9e8) = 0;
                    *(int*)(iVar5 + 0x9ec) = 0;
                    *(int*)(iVar5 + 0x9f0) = 0;
                    *(int*)(iVar5 + 0x9fc) = 0;
                }
                else {
                    uint DAT_00df8818 = FhUtil.get_at<uint>(0x9f8818);
                    uint DAT_00df881c = FhUtil.get_at<uint>(0x9f881c);
                    // Sets chr speed values depending on haste/slow, applies on-hit slow effect, DF8818 is the ATB speed pulled from the config setting
                    h_FUN_00634A20(uVar8, iVar5, DAT_00df8818);
                    *(int*)(iVar5 + 0x9fc) = (int)DAT_00df881c;
                }
            }


            uVar8 = uVar8 + 1;
            if (0x1e < (int)uVar8) {
                int* DAT_00df7f90 = FhUtil.ptr_at<int>(0x9F7F90);
                //data for if check
                byte DAT_00df8814 = FhUtil.get_at<byte>(0x9F8814);
                byte DAT_00df8816 = FhUtil.get_at<byte>(0x9F8816);
                //sub-menu open Wait flag
                byte DAT_00df8817 = FhUtil.get_at<byte>(0x9F8817);
                byte DAT_00df78a0 = FhUtil.get_at<byte>(0x9F78A0);
                byte DAT_00df78a3 = FhUtil.get_at<byte>(0x9F78A3);
                byte DAT_00df78a4 = FhUtil.get_at<byte>(0x9F78A4);

                h_MsMagicCheckCommandExe(DAT_00df7f90, 0xff, &local_8c, &local_90);
                bVar3 = true;
                if ((((DAT_00df8816 == '\0') && (DAT_00df78a4 == '\0')) && (DAT_00df8817 == '\0' && DAT_00df8814 == '\0')) && (local_8c != 2)) {
                    if ((DAT_00df78a0 != '\x01') || (DAT_00df78a3 != '\0')) {
                        bVar3 = false;
                    }
                    if ((local_8c == 0) && (bVar3)) {
                        iVar5 = 0;


                        do {
                            /* Update ATB for each character? */
                            uVar7 = h_MsGetChr((uint)iVar5);
                            iVar10 = h_msChrATBprocess((byte)iVar5, uVar7, (int*)local_84, iVar10);
                            iVar5 = iVar5 + 1;

                        } while (iVar5 < 0x1f);
                        local_88 = iVar10;
                        if (0 < iVar10) {
                            local_8c = 0;
                            while (true) {
                                uVar8 = 0xffffffff;
                                iVar10 = 0;
                                local_90 = 0;
                                if (0 < local_88) {
                                    do {
                                        uVar1 = local_84[iVar10];
                                        iVar5 = h_MsBtlChrNumCheck((byte)uVar1);//60ff90
                                        if (iVar5 != 0) {
                                            iVar5 = h_MsGetChr(uVar1);
                                            if (((int)uVar8 < 0) || (*(int*)(iVar5 + 0x9d8) < local_90)) {
                                                uVar8 = uVar1;
                                                local_90 = *(int*)(iVar5 + 0x9d8);
                                                local_8c = iVar10;
                                            }
                                        }
                                        iVar10 = iVar10 + 1;
                                    } while (iVar10 < local_88);
                                }
                                iVar10 = h_MsBtlChrNumCheck((byte)uVar8);//60ff90
                                if (iVar10 == 0) break;
                                local_84[local_8c] = 0xffffffff;
                                iVar10 = h_MsGetChr(uVar8);
                                iVar5 = h_MsActionRequest(uVar8, 0xff, 1, 1);//635300
                                if (iVar5 == 9) {
                                    *(byte*)(iVar10 + 0xe68) = 9;
                                }
                                else {
                                    cVar9 = *(char*)(iVar10 + 0xe69);
                                    iVar5 = (int)h_MsGetRamChrMonster(uVar8 & 0xff);
                                    byte DAT_00df78bf = FhUtil.get_at<byte>(0x9f78bf);
                                    cVar2 = DAT_00df78bf;
                                    /* Control Creatures debug flag check */
                                    if (iVar5 != 0) {
                                        byte DAT_00df78be = FhUtil.get_at<byte>(0x9f78be);
                                        cVar2 = DAT_00df78be;
                                        /* Control Enemies debug flag check */
                                    }
                                    if (cVar2 != '\0') {
                                        cVar9 = '\x01';
                                    }
                                    int DAT_00e13434 = FhUtil.get_at<int>(0xA13434);
                                    if (((*(uint*)(iVar10 + 0x434) & 0x40) != 0) && (DAT_00e13434 == 0)) {
                                        cVar9 = '\x04';
                                        *(byte*)(iVar10 + 0xE68) = 7;
                                    }

                                    if (((*(uint*)(iVar10 + 0x434) & 0x80) != 0) && (DAT_00e13434 == 0)) {
                                        cVar9 = '\x04';
                                        *(byte*)(iVar10 + 0xE68) = 8;
                                    }

                                    if (cVar9 == '\x01') {
                                        *(byte*)(iVar10 + 0xe68) = 4;
                                        /* Sets character state t 4 (can act, awaiting input?)
                                         *                       Show Menu function?
                                         *                       THIS TRIGGERED BREAK AND TRACE NEAR ATB FULL. */
                                        //h_TOBtlSetATBChr(uVar8, 0);//75d0c0
                                        h_TOBtlSetATBChr(uVar8);//75d0c0
                                    }
                                    else if (cVar9 == '\x02') {
                                        *(byte*)(iVar10 + 0xe68) = 5;
                                    }
                                    else if (cVar9 == '\x03') {
                                        *(byte*)(iVar10 + 0xe68) = 6;
                                    }
                                }
                            }
                        }
                        iVar10 = 0;
                        do {
                            iVar5 = h_MsGetChr((uint)iVar10);
                            uVar4 = *(byte*)(iVar5 + 0xe68);
                            switch (uVar4) {
                                case 5:
                                    uVar4 = (byte)h_MsActionRequest((uint)iVar10, 0xff, 0, 1);
                                    break;
                                case 6:
                                    //uVar4 = h_MsActionAI(iVar10, 0xff, 1);//648520
                                    uVar4 = (byte)h_MsActionAI((uint)iVar10);//648520
                                    break;
                                case 7:
                                    //uVar4 = h_MsAutoConfuseProcess(iVar10, iVar5);//649380
                                    uVar4 = (byte)h_MsAutoConfuseProcess((uint)iVar10);//649380
                                    break;
                                case 8:
                                    /* Returns 0 or 9 (escape handling?) */
                                    uVar4 = (byte)h_MsAutoBerserkProcess((uint)iVar10, iVar5);//649100
                                    break;
                            }
                            iVar10 = iVar10 + 1;
                            *(byte*)(iVar5 + 0xe68) = uVar4;
                        } while (iVar10 < 0x1f);
                    }
                }
                return;
            }
        } while (true);
    }
    

    public unsafe int h_msChrATBprocess(byte chr_id, int chr_base_address, int* param_3, int param_4) {
        bool bVar1;
        int iVar2;
        int iVar3;
        byte character_state;
        int local_8;


        iVar2 = chr_base_address;//local copy seemingly necessary for it not to crash on h_bravo_fx



        //checks some flags and returns early if they're set as below
        if (((chr_base_address == 0) || (*(byte*)(chr_base_address + 0x1789) == 0)) ||
        (*(byte*)(chr_base_address + 0xe67) != 0)) {
            return param_4;
        }

        //get character state variable (1 = can't act (ATB fill/Petrified/charging) etc.
        //4 is character can act, 9 when character is performing an action
        character_state = *(byte*)(chr_base_address + 0xe68);

        //if not in a certain state return early
        if (character_state != 0 && character_state != 1 && character_state != 2) { goto LAB_RETURN; }


        if (character_state == 0) {
            //need a pointer to this memory address for (h_MsMagicCheckCommandExe) first parameter
            int* DAT_00DF7F90 = FhUtil.ptr_at<int>(0x9F7F90);
            //data for if check
            byte DAT_00DF8814 = FhUtil.get_at<byte>(0x9F8814);
            byte DAT_00DF8816 = FhUtil.get_at<byte>(0x9F8816);
            //sub-menu open Wait flag
            byte DAT_00DF8817 = FhUtil.get_at<byte>(0x9F8817);
            byte DAT_00DF78A0 = FhUtil.get_at<byte>(0x9F78A0);
            byte DAT_00DF78A3 = FhUtil.get_at<byte>(0x9F78A3);
            byte DAT_00DF78A4 = FhUtil.get_at<byte>(0x9F78A4);

            //FUN_00644bb0(&DAT_00df7f90, 0xff, &param_2, &local_8);
            h_MsMagicCheckCommandExe(DAT_00DF7F90, 0xFF, &chr_base_address, &local_8);

            bVar1 = true;
            //check some flags including the sub-menu open wait flag (commented out) and return early if set
            if ((DAT_00DF8816 != '\0') || (DAT_00DF78A4 != '\0') ||
               /*(DAT_00DF8817 != '\0') ||*/ (DAT_00DF8814 != '\0') || (chr_base_address == 2)) goto LAB_RETURN;

            //more early return checks
            if ((DAT_00DF78A0 != '\x01') || (DAT_00DF78A3 != '\0')) {
                bVar1 = false;
            }
            if ((local_8 != 0) || (!bVar1)) goto LAB_RETURN;

            //update character state variable
            character_state = 1;
        }//end if character_state = 0 block

        if (character_state == 1) {

            int fill_check_bravo;
            //634b00
            //a sort of check if character's ATB is allowed to fill - mainly related to character's calculated ATB Speed value
            /*Will not return early if Stop/Sleep/Petrify are active and won't prevent characters with these from charging
             * - these statuses affect the speed value
             * in ChrAtbSpeedHandler*/
            //iVar2 is the character's base address stored in a var
            fill_check_bravo = h_bravo_fx(iVar2);/*this function checks if character is Active, Alive and the if the character's speed value (if 0 (i.e Stopped)). 
                                        * If not stopped etc - returns 1
                                        * otherwise returns 0
                                         */

            //return early if character's ATB not allowed to fill
            /* tentatively removed as the mod and custom_atb_progress() should handle statuses? - 06/04/2026
            if (fill_check_bravo == 0) {
                goto LAB_RETURN;
            }*/

            //custom ATB fill handling
            custom_atb_progress();

            //if ATB still not full then return early
            if (0 < *(int*)(iVar2 + 0x9d8)) goto LAB_RETURN;

            //oversoul handling
            h_MsCheckMonsterOversoul(chr_id);
            //Warrior Sentinel Handling
            h_MsResetDefenseStatus(chr_id);
            //Not sure what this does. It checks a flag that seems to be 0 msot of the time and does nothing.
            //636400
            h_MsClearDanceStatusMotion(chr_id);

            character_state = 2;

            /*This function checks whether the actor is in the enemy/creature section of the BattleChr data
             *and checks whether the 'Control Creatures' and 'Control Enemies' debug flags are set
             *Returns 1 if either of them are set
             *Returns 0 if neither of them are set
             */
            iVar3 = (int)h_MsGetRamChrMonster(chr_id);
            //if Control Creatures/Enemies is enabled
            if (iVar3 != 0) {
                //zero this
                *(int*)(iVar2 + 0x9f4) = 0;
            }

        }//end if character_state = 1 block
        else if (character_state != 2) {
            // all other states return
            goto LAB_RETURN;
        }


        iVar3 = h_MsCheckDanceStatus(chr_id); // fun_00636360(chr_id) - Usually returns 1
        //This block progresses characters on from state 2 -> 3, after this characters can move onto state 4 (Window showing) or onto acting 
        // + 9f4 is usually 0, so this runs most of the time (some pause/ delay buffer?)
        //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs
        if (*(int*)(iVar2 + 0x9f4) < 1) {
            //usually 1 so does run most of the time
            if (iVar3 != 0) {
                iVar3 = h_bravo_fx(iVar2);
                if (iVar3 != 0 && param_4 < 0x1F) {

                    //updates some table with the chr_id
                    param_3[param_4] = chr_id;
                    //*(int*)(param_3 + param_4 * 4) = chr_id; //Ghidra decomp original
                    *(byte*)(iVar2 + 0xe68) = 3; // progresses characters character_state to 3

                    return param_4 + 1;//returns 1

                }
            }
        }
        else {
            //some decrementing counter, but for what purpose?
            //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs - essentially, I don't want this block entered
            *(int*)(iVar2 + 0x9f4) = *(int*)(iVar2 + 0x9f4) - *(int*)(iVar2 + 0x9fc);
        }



    LAB_RETURN:
        //write character state and return
        *(byte*)(iVar2 + 0xe68) = character_state;
        return param_4;
    }

    //function that rewrites how ATB Progress is handled
    public void custom_atb_progress() {
        //define an array will hold a flag for each character that says whether their ATB can charge or not
        int[] can_fill_array = new int[31];
        //an array to hold each characters time until ATB full value
        int[] atb_timer_values = new int[31];

        bool chrCanAct = false;

        do {

            //fill up arrays
            for (int i = 0; i < can_fill_array.Length; i++) {
                int chr_base_addr = h_MsGetChr((uint)i);
                can_fill_array[i] = h_bravo_fx(chr_base_addr); // fill up can_fill array

                //alter can_fill array
                if (can_fill_array[i] == 0) {
                    //check for sleep
                    if ((*(uint*)(chr_base_addr + 0x434) >> 2 & 1) == 1) {
                        can_fill_array[i] = 1; // pretend they can fill
                    }//check for stop
                    if (*(sbyte*)(chr_base_addr + 0x43e) > 0) {
                        can_fill_array[i] = 1; // pretend they can fill
                    }
                }

                atb_timer_values[i] = *(int*)(chr_base_addr + 0x9d8); // fill up ATB timer values array

            }

            // Find lowest time remaining value of the characters whos ATBS are allowed to charge
            int winningIndex = -1;
            int bestValue = int.MaxValue;
            //cycle through the chr can_fill array
            for (int i = 0; i < can_fill_array.Length; i++) {
                //where the character is allowed to fill, update time remaining variable
                if (can_fill_array[i] == 1) {
                    int time_remaining = atb_timer_values[i];
                    //if the time remaining is lower than the current lowest recorded, update with the new lowest
                    if (time_remaining < bestValue) {
                        bestValue = time_remaining;
                        winningIndex = i;
                    }
                }
            }

            //update ATBs
            //If a at least 1 character's ATB is allowed to fill then
            if (winningIndex != -1) {
                //cycle through the Can_Fill array
                for (int i = 0; i < can_fill_array.Length; i++) {
                    /*where the character's ATB is allowed to fill, get their address and subtract the smallest
                     * ATB Time remaining for all characters from their remaining ATB time*/
                    if (can_fill_array[i] == 1) {
                        int chr_addr = h_MsGetChr((uint)i);
                        *(int*)(chr_addr + 0x9d8) = *(int*)(chr_addr + 0x9d8) - bestValue;
                    }
                }

                int winningIndexChrBase = h_MsGetChr((uint)winningIndex);

                if (h_bravo_fx(winningIndexChrBase) == 1)
                { 
                    chrCanAct = true;
                }
                else {
                    *(int*)(winningIndexChrBase + 0x9d8) = cannnotActRestTime((uint)winningIndex, 0x302c);
                    *(int*)(winningIndexChrBase + 0x9dc) = cannnotActRestTime((uint)winningIndex, 0x302c);
                    TbCantActStatusProcess((uint)winningIndex); // decrement their Sleep and Stop turns remaining.
                }
            }
            else {
                //_fill_logger.Info("No characters eligible for ATB fill.");
                // This else block handles situations where NO character can act, due to debugs flags, or all being Asleep or Stopped.
                
                // Debug flag handling
                bool dbg_allies_disabled = FhUtil.get_at<byte>(0x9F78BA) == 1;
                bool dbg_enemies_disabled = FhUtil.get_at<byte>(0x9F78BB) == 1;
                if(dbg_allies_disabled && dbg_enemies_disabled) { chrCanAct = true;  }

                // Status handling
                for (uint x = 0; x < atb_timer_values.Length; x++) {
                    TbCantActStatusProcess(x);
                    int chr_addr = h_MsGetChr(x);
                    uint base_atb_value = FhUtil.get_at<uint>(0x9f8818);
                    h_FUN_00634A20(x, chr_addr, base_atb_value); // recalculate the units ATB speed value - 0 -> no fill, 95 -> can fill again
                    if (h_bravo_fx(chr_addr) == 1) { //h_bravo checks flags, and chrs ATB fill speed value
                        chrCanAct = true;
                        break;
                    }
                }
            }
            //logging  
            //_fill_logger.Info("Can fill array: " + string.Join("", can_fill));
            //_fill_logger.Info("ATB remaining: " + string.Join(" / ", atb_timer_values));


        } while (!chrCanAct);

    }

    // Calcs character's underlying speed values, handled Haste/Slow fill speed multiplier in vanilla, as well as on hit ATB slowdown - effects removed
    public unsafe uint h_FUN_00634A20(uint chr_id, int chr_base_addr, uint speed_value) {

        uint uVar1;
        uint animation_speed;

        //Negative status adjustments
        //returns a bitfield where bit 0 is Stop status, bit 1 is petrify status and bit 2 is Sleep status
        uVar1 = h_MsStatCheckStop((byte)chr_id, 0);// FUN_006430f0(chr_id, 0);

        //if either Stop or Petrify bit is set, freeze the animation
        if ((uVar1 & 0x03) != 0) {
            animation_speed = 0;
        }
        else {
            //if neither Stopped or Petrified value is the same as atb_speed
            animation_speed = speed_value;
        }

        //if not under any of the three statuses, value is the ATB speed
        if (uVar1 == 0) {
            uVar1 = speed_value;
        }
        else {
            //if under any of the three statuses, value is 0
            uVar1 = 0;
        }

        //Write the values
        //speed value 2 - ?
        *(uint*)(chr_base_addr + 0x9f0) = speed_value;
        //speed value 3 - writes the status or animation speed?
        *(uint*)(chr_base_addr + 0x9ec) = animation_speed;
        //speed value 4 - This allows statuses to decrement?
        *(uint*)(chr_base_addr + 0x9e8) = uVar1;

        //speed value 1 - Write the character's ATB countdown per tick and return
        *(uint*)(chr_base_addr + 0x9e4) = uVar1;
        return uVar1;
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        //_MsChrATBprocess_handle.hook(); // Uncomment and add to return list if required

        // ATB fill hooks
        return _msChrATBprocess_handle.hook()
        && _FUN_00634A20_handle.hook()
        && _MsMagicCheckCommandExe_handle.hook()
        && _MsBtlChrNumCheck_handle.hook()
        && _MsActionRequest_handle.hook()
        && _TOBtlSetATBChr_handle.hook()
        && _MsActionAI_handle.hook()
        && _MsAutoConfuseProcess_handle.hook()
        && _MsAutoBerserkProcess_handle.hook()
        && _MsGetRamChrMonster_handle.hook()
        // Common hooks
        && _MsGetChr_handle.hook()
        && _MsGetComData_handle.hook()
        //Status Handling hooks
        && _MsStatCheckStop_handle.hook()
        && _MsATBActiveCheck_handle.hook()
        && _FUN_006218E0_handle.hook()
        && _ClampBetween_handle.hook()
        && _FUN_00636690_handle.hook()
        && _MsStructClear_handle.hook()
        && _MsDamageBufferExe_handle.hook()
        && _MsSetStatus_handle.hook()
        && _MsSetChrWeak_handle.hook()
        && _MsStatusEffectCheck_handle.hook()
        && _MsMotionRecoverExe_handle.hook();
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
