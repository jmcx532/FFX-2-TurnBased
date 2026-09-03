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

    /*
    //delegates
    //6343d0 - MsChrATBprocess
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void MsChrATBprocess();
    private static FhMethodHandle<MsChrATBprocess> _MsChrATBprocess =>
        new ( new FhMethodLocation("FFX-2.exe", 0x2343D0) );//6343d0

    ////625bf0 - MsGetRamChrMonster
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsGetRamChrMonster(byte chr_id);
    private static FhMethodHandle<MsGetRamChrMonster> _MsGetRamChrMonster =>
        new ( new FhMethodLocation("FFX-2.exe", 0x225BF0) );//625bf0

    //634a20 - non-existent in Switch ver - Alters Chr ATB Speed values: Haste and Slow, Sleep/Stop/Stone, on-hit effect
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsChrSetDecTime(uint chr_id, int chr_base_addr, uint cfg_atb_speed);
    private static FhMethodHandle<MsChrSetDecTime> _MsChrSetDecTime =>
        new ( new FhMethodLocation("FFX-2.exe", 0x234A20) );//634b40

    //644bb0 - MsMagicCheckCommandExe
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsMagicCheckCommandExe(int param_1, uint param_2, int* param_3, int* param_4);
    private static FhMethodHandle<MsMagicCheckCommandExe> _MsMagicCheckCommandExe =>
        new ( new FhMethodLocation("FFX-2.exe", 0x244BB0) );//644bb0

    //60ff90
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsBtlChrNumCheck(byte chr_id);
    private static FhMethodHandle<MsBtlChrNumCheck> _MsBtlChrNumCheck =>
        new ( new FhMethodLocation("FFX-2.exe", 0x20FF90) );//60ff90

    //635300
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsActionRequest(uint chr_id, int param_2, int param_3, int param_4);
    private static FhMethodHandle<MsActionRequest> _MsActionRequest =>
        new ( new FhMethodLocation("FFX-2.exe", 0x235300) );//635300

    //75d0c0
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TOBtlSetATBChr(byte chr_id);
    private static FhMethodHandle<TOBtlSetATBChr> _TOBtlSetATBChr =>
        new ( new FhMethodLocation("FFX-2.exe", 0x35D0C0) );//75d0c0

    //648520
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate byte MsActionAI(uint chr_id);//this may be missing param_2 and param_3
    private static FhMethodHandle<MsActionAI> _MsActionAI =>
        new ( new FhMethodLocation("FFX-2.exe", 0x248520) );//648520

    //649100
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsAutoBerserkProcess(uint chr_id, int chr_base_address);
    private static FhMethodHandle<MsAutoBerserkProcess> _MsAutoBerserkProcess =>
        new ( new FhMethodLocation("FFX-2.exe", 0x249100) );//649100


    //634b40 - msChrATBprocess
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int msChrATBprocess(byte chr_id, int chr_base_address, int* param_3, int param_4);
    private static FhMethodHandle<msChrATBprocess> _msChrATBprocess =>
        new ( new FhMethodLocation("FFX-2.exe", 0x234B40) );//634b40

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint FUN_634B00(int param_1);
    private static FhMethodHandle<FUN_634B00> _FUN_634B00 =>
        new ( new FhMethodLocation("FFX-2.exe", 0x234B00) );

    //61c290 - MsCheckMonsterOversoul
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckMonsterOversoul(uint param_1);
    private static FhMethodHandle<MsCheckMonsterOversoul> _MsCheckMonsterOversoul =>
        new ( new FhMethodLocation("FFX-2.exe", 0x21C290) );

    //636900 -  Warriors Sentinel related - MsResetDefenseStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsResetDefenseStatus(byte param_1);
    private static FhMethodHandle<MsResetDefenseStatus> _MsResetDefenseStatus =>
        new ( new FhMethodLocation("FFX-2.exe", 0x236900) );

    //636400
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsClearDanceStatusMotion(byte param_1);
    private static FhMethodHandle<MsClearDanceStatusMotion> _MsClearDanceStatusMotion =>
        new ( new FhMethodLocation("FFX-2.exe", 0x236400) );

    //636360 - MsCheckDanceStatus
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsCheckDanceStatus(byte param_1);
    private static FhMethodHandle<MsCheckDanceStatus> _MsCheckDanceStatus =>
        new ( new FhMethodLocation("FFX-2.exe", 0x236360) );

    //611450 - MsGetChr
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetChr(uint chr_id);
    private static FhMethodHandle<MsGetChr> _MsGetChr =>
        new ( new FhMethodLocation("FFX-2.exe", 0x211450) );//611450

    */

    //649380
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate uint MsAutoConfuseProcess(uint chr_id);//this may be missing a param_2
    private static FhMethodHandle<MsAutoConfuseProcess> _MsAutoConfuseProcess =>
        new(new FhMethodLocation("FFX-2.exe", 0x249380));//649380


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MsGetComData(uint arg1, byte* arg2);
    //625160 - MsGetComData (delegate declared in ATBFill_StatusHandling.cs)
    private static FhMethodHandle<MsGetComData> _MsGetComData =>
        new(new FhMethodLocation("FFX-2.exe", 0x225160));

    //634b40 - msChrATBprocess
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int msChrATBprocess(byte chr_id, Chr* chr, int* param_3, int param_4);
    private static FhMethodHandle<msChrATBprocess> _msChrATBprocess =>
        new(new FhMethodLocation("FFX-2.exe", 0x234B40));//634b40

    public ATBFillModule() { }

    public Chr* h_MsGetChr(uint param_1) {
        return FFX2.FhCall.MsGetChr.chain_from(h_MsGetChr).fnptr!(param_1);
    }
    public unsafe byte h_MsMagicCheckCommandExe(int param_1, uint param_2, int* param_3, int* param_4) {
        return FFX2.FhCall.MsMagicCheckCommandExe.chain_from(h_MsMagicCheckCommandExe).fnptr!(param_1, param_2, param_3, param_4);
    }
    public uint h_MsBtlChrNumCheck(uint chr_id) {
        return FFX2.FhCall.MsBtlChrNumCheck.chain_from(h_MsBtlChrNumCheck).fnptr!(chr_id);
    }
    public uint h_MsActionRequest(uint chr_id, int param_2, int param_3, int param_4) {
        return FFX2.FhCall.MsActionRequest.chain_from(h_MsActionRequest).fnptr!(chr_id, param_2, param_3, param_4);
    }
    public void h_TOBtlSetATBChr(byte chr_id) {
        FFX2.FhCall.TOBtlSetATBChr.chain_from(h_TOBtlSetATBChr).fnptr!(chr_id);
    }

    //arg2 = 0xff, arg3 = 1
    public uint h_MsActionAI(uint chr_id, int arg2, int arg3) {
        return FFX2.FhCall.MsActionAI.chain_from(h_MsActionAI).fnptr!(chr_id, arg2, arg3);
    }
    public uint h_MsAutoConfuseProcess(uint chr_id) {
        return _MsAutoConfuseProcess.chain_from(h_MsAutoConfuseProcess).fnptr!(chr_id);
    }
    public uint h_MsAutoBerserkProcess(uint chr_id, Chr* chr) {
        return FFX2.FhCall.MsAutoBerserkProcess.chain_from(h_MsAutoBerserkProcess).fnptr!(chr_id, chr);
    }
    public uint h_MsGetRamChrMonster(uint chr_id) {
        return FFX2.FhCall.MsGetRamChrMonster.chain_from(h_MsGetRamChrMonster).fnptr!(chr_id);
    }
    public uint h_FUN_00634B00(Chr* param_1) {
        /* -- restore this for vanilla turn-based to fix Yojimbo's Daigoro not taking action
        uint original_result = FFX2.FhCall.FUN_00634B00.chain_from(h_FUN_00634B00).fnptr!(param_1);

        if (original_result == 1)
        {
            int chr_addr = (int)param_1;
            if (*(byte*)(chr_addr + 0x1788) == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            return 0;
        }*/

        return FFX2.FhCall.FUN_00634B00.chain_from(h_FUN_00634B00).fnptr!(param_1);
    }
    public void h_MsCheckMonsterOversoul(uint param_1) {
        FFX2.FhCall.MsCheckMonsterOversoul.chain_from(h_MsCheckMonsterOversoul).fnptr!(param_1);
    }
    public uint h_MsResetDefenseStatus(uint param_1) {
        return FFX2.FhCall.MsResetDefenseStatus.chain_from(h_MsResetDefenseStatus).fnptr!(param_1);
    }
    public uint h_MsClearDanceStatusMotion(uint param_1) {
        return FFX2.FhCall.MsClearDanceStatusMotion.chain_from(h_MsClearDanceStatusMotion).fnptr!(param_1);
    }
    public uint h_MsCheckDanceStatus(uint param_1) {
        return FFX2.FhCall.MsCheckDanceStatus.chain_from(h_MsCheckDanceStatus).fnptr!(param_1);
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
        int* local_8c = null;
        int local_88;
        //uint local_84 [31];
        //uint[] local_84 = new uint[31];
        uint* local_84 = stackalloc uint[31];
        //uint local_8;

        iVar10 = 0;
        uVar8 = 0;
        do {

            Chr* chr = h_MsGetChr(uVar8);
            iVar5 = (int)chr;
            isEnemy = (int)h_MsGetRamChrMonster((byte)uVar8);

            if (isEnemy == 1) {
                byte DAT_00df78bb = FhUtil.get_at<byte>(0x9f78bb);
                if (DAT_00df78bb != 0 || *(byte*)(iVar5 + 0x1789) == 0) {

                    chr = h_MsGetChr(uVar8 & 0xff);
                    iVar5 = (int)chr;

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
                    h_MsChrSetDecTime(uVar8, chr, DAT_00df8818);
                    *(int*)(iVar5 + 0x9fc) = (int)DAT_00df881c;
                }
            }
            if (isEnemy == 0) {
                byte DAT_00df78ba = FhUtil.get_at<byte>(0x9f78ba);
                if (DAT_00df78ba != 0 || *(byte*)(iVar5 + 0x1789) == 0) {
                    chr = h_MsGetChr(uVar8 & 0xff);
                    iVar5 = (int)chr;
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
                    h_MsChrSetDecTime(uVar8, chr, DAT_00df8818);
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

                h_MsMagicCheckCommandExe((int)DAT_00df7f90, 0xff, local_8c, &local_90);
                bVar3 = true;
                if ((((DAT_00df8816 == '\0') && (DAT_00df78a4 == '\0')) && (DAT_00df8817 == '\0' && DAT_00df8814 == '\0')) && ((int)local_8c != 2)) {
                    if ((DAT_00df78a0 != '\x01') || (DAT_00df78a3 != '\0')) {
                        bVar3 = false;
                    }
                    if (((int)local_8c == 0) && (bVar3)) {
                        iVar5 = 0;


                        do {
                            /* Update ATB for each character? */
                            Chr* chr2 = h_MsGetChr((uint)iVar5);
                            uVar7 = (int)chr2;
                            iVar10 = h_msChrATBprocess((byte)iVar5, chr2, (int*)local_84, iVar10);
                            iVar5 = iVar5 + 1;

                        } while (iVar5 < 0x1f);
                        local_88 = iVar10;
                        if (0 < iVar10) {
                            int iVar20 = 0;
                            while (true) {
                                uVar8 = 0xffffffff;
                                iVar10 = 0;
                                local_90 = 0;
                                if (0 < local_88) {
                                    do {
                                        uVar1 = local_84[iVar10];
                                        iVar5 = (int)(h_MsBtlChrNumCheck((byte)uVar1));// 60ff90
                                        if (iVar5 != 0) {
                                            chr = h_MsGetChr(uVar1);
                                            iVar5 = (int)chr;
                                            if (((int)uVar8 < 0) || (*(int*)(iVar5 + 0x9d8) < local_90)) {
                                                uVar8 = uVar1;
                                                local_90 = *(int*)(iVar5 + 0x9d8);
                                                iVar20 = iVar10;
                                            }
                                        }
                                        iVar10 = iVar10 + 1;
                                    } while (iVar10 < local_88);
                                }
                                iVar10 = (int)h_MsBtlChrNumCheck((byte)uVar8);// 60ff90
                                if (iVar10 == 0) break;
                                local_84[iVar20] = 0xffffffff;

                                Chr* chr3 = h_MsGetChr(uVar8);
                                iVar10 = (int)chr3;
                                iVar5 = (int)h_MsActionRequest(uVar8, 0xff, 1, 1);// 635300
                                if (iVar5 == 9) {
                                    *(byte*)(iVar10 + 0xe68) = 9;
                                }
                                else {
                                    cVar9 = *(char*)(iVar10 + 0xe69);
                                    iVar5 = (int)h_MsGetRamChrMonster((byte)(uVar8 & 0xff));
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
                                        h_TOBtlSetATBChr((byte)uVar8);//75d0c0
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
                            Chr* chr4 = h_MsGetChr((uint)iVar10);
                            iVar5 = (int)chr4;
                            uVar4 = *(byte*)(iVar5 + 0xe68);
                            switch (uVar4) {
                                case 5:
                                    uVar4 = (byte)h_MsActionRequest((uint)iVar10, 0xff, 0, 1);
                                    break;
                                case 6:
                                    //uVar4 = h_MsActionAI(iVar10, 0xff, 1);//648520
                                    uVar4 = (byte)h_MsActionAI((uint)iVar10, 0xff, 1);//648520
                                    break;
                                case 7:
                                    //uVar4 = h_MsAutoConfuseProcess(iVar10, iVar5);//649380
                                    uVar4 = (byte)h_MsAutoConfuseProcess((uint)iVar10);//649380
                                    break;
                                case 8:
                                    /* Returns 0 or 9 (escape handling?) */
                                    uVar4 = (byte)h_MsAutoBerserkProcess((uint)iVar10, chr4);//649100
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
    

    public unsafe int h_msChrATBprocess(byte chr_id, Chr* chr_base_address, int* param_3, int param_4) {
        bool bVar1;
        Chr* iVar2;
        int iVar3;
        byte character_state;
        int local_8;



        iVar2 = chr_base_address;//local copy seemingly necessary for it not to crash on h_FUN_634B00



        //checks some flags and returns early if they're set as below
        if ((((int)chr_base_address == 0) || (*(byte*)((int)chr_base_address + 0x1789) == 0)) ||
        (*(byte*)((int)chr_base_address + 0xe67) != 0)) {
            return param_4;
        }

        //get character state variable (1 = can't act (ATB fill/Petrified/charging) etc.
        //4 is character can act, 9 when character is performing an action
        character_state = *(byte*)((int)chr_base_address + 0xe68);

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
            h_MsMagicCheckCommandExe((int)DAT_00DF7F90, 0xFF, (int*)chr_base_address, &local_8);

            bVar1 = true;
            //check some flags including the sub-menu open wait flag (commented out) and return early if set
            if ((DAT_00DF8816 != '\0') || (DAT_00DF78A4 != '\0') ||
               /*(DAT_00DF8817 != '\0') ||*/ (DAT_00DF8814 != '\0') || ((int)chr_base_address == 2)) goto LAB_RETURN;

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
            fill_check_bravo = (int)h_FUN_00634B00(iVar2);/*this function checks if character is Active, Alive and the if the character's speed value (if 0 (i.e Stopped)). 
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
            if (0 < *(int*)((int)iVar2 + 0x9d8)) goto LAB_RETURN;

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
                *(int*)((int)iVar2 + 0x9f4) = 0;
            }

        }//end if character_state = 1 block
        else if (character_state != 2) {
            // all other states return
            goto LAB_RETURN;
        }


        iVar3 = (int)h_MsCheckDanceStatus(chr_id); // fun_00636360(chr_id) - Usually returns 1
        //This block progresses characters on from state 2 -> 3, after this characters can move onto state 4 (Window showing) or onto acting 
        // + 9f4 is usually 0, so this runs most of the time (some pause/ delay buffer?)
        //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs
        if (*(int*)((int)iVar2 + 0x9f4) < 1) {
            //usually 1 so does run most of the time
            if (iVar3 != 0) {
                iVar3 = (int)h_FUN_00634B00(iVar2);
                if (iVar3 != 0 && param_4 < 0x1F) {

                    //updates some table with the chr_id
                    param_3[param_4] = chr_id;
                    //*(int*)(param_3 + param_4 * 4) = chr_id; //Ghidra decomp original
                    *(byte*)((int)iVar2 + 0xe68) = 3; // progresses characters character_state to 3

                    return param_4 + 1;//returns 1

                }
            }
        }
        else {
            //some decrementing counter, but for what purpose?
            //if buffer not 0, causes regen/poison bug fixed in BugCausingFXModule.cs - essentially, I don't want this block entered
            *(int*)((int)iVar2 + 0x9f4) = *(int*)((int)iVar2 + 0x9f4) - *(int*)((int)iVar2 + 0x9fc);
        }



    LAB_RETURN:
        //write character state and return
        *(byte*)((int)iVar2 + 0xe68) = character_state;
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
                Chr* chr = h_MsGetChr((uint)i);
                int chr_base_addr = (int)chr;
                can_fill_array[i] = (int)h_FUN_00634B00(chr); // fill up can_fill array

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
                        Chr* chr2 = h_MsGetChr((uint)i);
                        int chr_addr = (int)chr2;
                        *(int*)(chr_addr + 0x9d8) = *(int*)(chr_addr + 0x9d8) - bestValue;
                    }
                }

                Chr* chr3 = h_MsGetChr((uint)winningIndex);
                int winningIndexChrBase = (int)chr3;

                if (h_FUN_00634B00(chr3) == 1)
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
                // This else block handles situations where NO character can act, due to debugs flags, or all being Asleep or Stopped.
                
                // Debug flag handling
                bool debugAlliesDisabled = FhUtil.get_at<byte>(0x9F78BA) == 1;
                bool dbgEnemiesDisabled = FhUtil.get_at<byte>(0x9F78BB) == 1;
                if(debugAlliesDisabled && dbgEnemiesDisabled) { chrCanAct = true;  }

                // Status handling
                for (uint x = 0; x < atb_timer_values.Length; x++) {
                    TbCantActStatusProcess(x);
                    Chr* chr = h_MsGetChr(x);
                    int chr_addr = (int)chr;
                    uint base_atb_value = FhUtil.get_at<uint>(0x9f8818);
                    h_MsChrSetDecTime(x, chr, base_atb_value); // recalculate the units ATB speed value - 0 -> no fill, 95 -> can fill again
                    if (h_FUN_00634B00(chr) == 1) { //h_bravo checks flags, and chrs ATB fill speed value
                        chrCanAct = true;
                        break;
                    }
                }
            }

        } while (!chrCanAct);

    }

    // Calcs character's underlying speed values, handled Haste/Slow fill speed multiplier in vanilla, as well as on hit ATB slowdown - effects removed
    public unsafe uint h_MsChrSetDecTime(uint chr_id, Chr* chr, uint speed_value) {

        uint uVar1;
        uint animation_speed;

        int chr_base_addr = (int)chr;

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
        //FFX2.FhCall.MsChrATBprocess.hook(this, h_MsChrATBprocess); // Uncomment and add to return list if required

        // ATB fill hooks
        return _msChrATBprocess.hook(this, h_msChrATBprocess)
        && FFX2.FhCall.MsChrSetDecTime.hook(this, h_MsChrSetDecTime)
        && FFX2.FhCall.MsMagicCheckCommandExe.hook(this, h_MsMagicCheckCommandExe)
        && FFX2.FhCall.MsBtlChrNumCheck.hook(this, h_MsBtlChrNumCheck)
        && FFX2.FhCall.MsActionRequest.hook(this, h_MsActionRequest)
        && FFX2.FhCall.TOBtlSetATBChr.hook(this, h_TOBtlSetATBChr)
        && FFX2.FhCall.MsActionAI.hook(this, h_MsActionAI)
        && _MsAutoConfuseProcess.hook(this, h_MsAutoConfuseProcess)
        && FFX2.FhCall.MsAutoBerserkProcess.hook(this, h_MsAutoBerserkProcess)
        && FFX2.FhCall.MsGetRamChrMonster.hook(this, h_MsGetRamChrMonster)
        && FFX2.FhCall.FUN_00634B00.hook(this, h_FUN_00634B00)
        && FFX2.FhCall.MsCheckMonsterOversoul.hook(this, h_MsCheckMonsterOversoul)
        && FFX2.FhCall.MsCheckDanceStatus.hook(this, h_MsCheckDanceStatus)
        && FFX2.FhCall.MsClearDanceStatusMotion.hook(this, h_MsClearDanceStatusMotion)
        && FFX2.FhCall.MsResetDefenseStatus.hook(this, h_MsResetDefenseStatus)

        // Common hooks
        && FFX2.FhCall.MsGetChr.hook(this, h_MsGetChr)
        && _MsGetComData.hook(this, h_MsGetComData)
        //Status Handling hooks
        && FFX2.FhCall.MsStatCheckStop.hook(this, h_MsStatCheckStop)
        && FFX2.FhCall.MsATBActiveCheck.hook(this, h_MsATBActiveCheck)
        && FFX2.FhCall.MsCheckStatCount.hook(this, h_MsCheckStatCount)
        && FhCall.MsCheckRange.hook(this, h_MsCheckRange)
        && FFX2.FhCall.FUN_00636690.hook(this, h_FUN_00636690)
        && FFX2.FhCall.MsStructClear.hook(this, h_MsStructClear)
        && FFX2.FhCall.MsDamageBufferExe.hook(this, h_MsDamageBufferExe)
        && FFX2.FhCall.MsSetStatus.hook(this, h_MsSetStatus)
        && FFX2.FhCall.MsSetChrWeak.hook(this, h_MsSetChrWeak)
        && FFX2.FhCall.MsStatusEffectCheck.hook(this, h_MsStatusEffectCheck)
        && FFX2.FhCall.MsMotionRecoverExe.hook(this, h_MsMotionRecoverExe);
    }

    public override void load_local_state(FileStream? local_state_file, FhLocalStateInfo local_state_info) { }
    public override void save_local_state(FileStream local_state_file) { }
}
