namespace Fahrenheit.Modules.FFX2TurnBased;

public unsafe partial class ATBFillModule : FhModule
{
    public int cannnotActRestTime(uint chr_id, uint command_id)
    {
        int chr_base_address;
        int cmd_base_address;

        /* Gets the character's base address */
        Chr* chr = FFX2.FhCall.MsGetChr.fnptr!(chr_id);
        chr_base_address = (int)chr;
        // FUN_00625160 - Get the commands base address, this function can also return other Excel data types
        cmd_base_address = _MsGetComData.fnptr!(command_id, (byte*)(0));

        //normal calculation
        //read the commands atb_cost and multiply
        int cmd_recovery_time = (int)(*(ushort*)(cmd_base_address + 0x22) * 10000);

        //divide that by (user's Agility + 1) -- VANILLA
        //uint agility_divisor = (uint)(*(byte*)(chr_base_address + 0x39a)) + 1;

        //CUSTOM DIVISOR
        byte agility = (*(byte*)(chr_base_address + 0x39a));
        double divisor;
        //if agility is over 100, use a stronger taper that means higher agility stats don't reduce recovery time as much.
        if (agility > 100)
        {
            divisor = Math.Pow(agility + 125, 0.85) + 1;
        }
        else
        {
            //if agility is 99 or less, use vanilla divisor
            divisor = agility + 1;
        }

        uint agility_divisor = (uint)Math.Round(divisor);



        //delay from attacks to be added
        uint accrued_delay = (uint)*(int*)(chr_base_address + 0x9e0);
        //calculate ATB timer length and clamp between 0 and 99999
        int calced_recovery = int.Clamp((int)((cmd_recovery_time / agility_divisor) + accrued_delay), 0, 99999);

        //Haste / Slow Modifier
        //if character is hasted - half recovery time
        if (*(byte*)(chr_base_address + 0x43c) != '\0')
        {
            calced_recovery = calced_recovery / 2;
        }
        //if character is slowed - double recovery time
        if (*(byte*)(chr_base_address + 0x43d) != '\0')
        {
            calced_recovery = calced_recovery * 2;
        }


        //auto ability recovery time reduction
        ushort command_used = (*(ushort*)(chr_base_address + 0xf3c));
        int percent_reduction = 0;

        //apply auto ability reduction
        calced_recovery = ((100 - percent_reduction) * calced_recovery) / 100;

        // Accrued delay is reset
        *(int*)(chr_base_address + 0x9e0) = 0;


        //return calculated recovery time to be written
        return calced_recovery;
    }

    public void TbCantActStatusProcess(uint chr_id)
    {
        int chr_base;
        uint status_bitfield2;
        int sleep_bit_cleared;
        int sleep_expired1;
        int sleep_expired2;

        //uint chr_id;
        uint ChrSpeedVal4;

        int ChrSpeedVal3 = 0;

        Chr* chr = FFX2.FhCall.MsGetChr.fnptr!(chr_id);// Get Chr base address
        chr_base = (int)chr;
        if ((((*(byte*)(chr_base + 0x1784) != 0) && (*(byte*)(chr_base + 0x1792) == 0)) && (0 < *(int*)(chr_base + 0x3b4))) &&
       ((*(byte*)(chr_base + 0x1787) == 0 && ((*(byte*)(chr_base + 0x434) & 2) == 0))))
        {

            // Handle Stop
            // If Stop timer has time remaining
            if (*(sbyte*)(chr_base + 0x4ba) > 0)
            {
                *(sbyte*)(chr_base + 0x4ba) = (sbyte)(*(sbyte*)(chr_base + 0x4ba) - 1);
                *(sbyte*)(chr_base + 0x43e) = (sbyte)(*(sbyte*)(chr_base + 0x43e) - 1);
            }

            // read the character's speed value
            ChrSpeedVal4 = *(uint*)(chr_base + 0x9f0);
            sleep_expired1 = 0;
            sleep_expired2 = 0;
            sleep_bit_cleared = 0;

            // Read Chrs second copy of status bitfield
            status_bitfield2 = *(uint*)(chr_base + 0x450);

            bool isAsleep = (*(uint*)(chr_base + 0x434) >> 2 & 1) == 1;// check sleep state
            int sleep_off_count = *(int*)(chr_base + 0x45c);


            // Handle Sleep
            if ((isAsleep && (0 < sleep_off_count)))
            {

                int new_off_count = (int)(sleep_off_count - ChrSpeedVal4);// compute status time remaining

                //if status has expired
                if (new_off_count < 0)
                {

                    sleep_expired1 = sleep_expired1 + 1;
                    sleep_expired2 = sleep_expired2 + 1;
                    //new_off_count = 0;
                    //if sleep
                    if ((status_bitfield2 & 4) != 0)
                    {
                        //increment local_38
                        sleep_bit_cleared = sleep_bit_cleared + 1;
                        // unset bit for sleep here
                        status_bitfield2 &= ~(1u << 2);
                    }

                }
                *(uint*)(chr_base + 0x45c) = (uint)new_off_count;// write the updated value

                // Handle Doom while asleep
                sbyte doom_count = *(sbyte*)(chr_base + 0x446);

                if (doom_count > 0)   // if character is Doomed
                {
                    sbyte new_doom_count = (sbyte)(doom_count - 1);
                    if (new_doom_count < 2)
                    {
                        uint statIdx = FFX2.FhCall.MsCheckStatCount.fnptr!(14);
                        *(sbyte*)(chr_base + 0x4c2) = sbyte.Clamp(new_doom_count, 0, 0x7d);
                        FFX2.FhCall.FUN_00236660.fnptr!(chr_id, chr, (byte)statIdx);
                    }
                    else
                    {
                        *(sbyte*)(chr_base + 0x446) = sbyte.Clamp(new_doom_count, 0, 0x7d);
                        *(sbyte*)(chr_base + 0x4c2) = sbyte.Clamp(new_doom_count, 0, 0x7d);
                    }
                }

                    *(uint*)(chr_base + 0x450) = status_bitfield2;// write the character's updated status bitfield

                    //Poison Handling

                    int* piVar8;
                    ChrSpeedVal3 = *(int*)(chr_base + 0x9ec);

                    bool isPoisoned = (*(uint*)(chr_base + 0x434) >> 5 & 1) == 1;// check poison state
                    if (isPoisoned)
                    {
                        piVar8 = (int*)(chr_base + 0x684);// Get pointer to Chr poison accumulator time value
                        *piVar8 = *piVar8 + (int)ChrSpeedVal3;//Write or increase the accumulator by the Chrs Speed Value
                        int psn_accumulator_val = *(int*)(chr_base + 0x684);// Read the updated accumulator

                        // If the accumulator value is greater than the threshold value
                        if (*(int*)(chr_base + 0x68c) < psn_accumulator_val)
                        {
                            int psn_damage_amount = 0;
                            // Accumulator value is written as: previously read accumulator value - the threshold value
                            *(int*)(chr_base + 0x684) = psn_accumulator_val - *(int*)(chr_base + 0x68c);

                            //psn_damage_amount = (*(int*)(chr_base + 0x694) * *(int*)(chr_base + 0x384)) >> 8; // Poison Dmg Calc: (X/256) * MaxHP
                            psn_damage_amount = (int)((*(int*)(chr_base + 0x694) / 256.0) * *(int*)(chr_base + 0x384));


                            //byte[] local_1c_psn = new byte[0x14];
                            DamageBuffer psn_buffer = new();
                            DamageBuffer* pBuffer = &psn_buffer;

                            //FFX2.FhCall.MsStructClear.fnptr!(pBuffer, 0x14);//62a0f0
                                                           //local_18 = 0x100ff;
                            psn_buffer.com_id = 0xff;
                            psn_buffer.target_stat = 0x01;
                            //local_1c[0] = (byte)chr_id;
                            psn_buffer.chr_id = (byte)chr_id;
                            //local_14 = psn_damage_amount;
                            psn_buffer.damage_hp = psn_damage_amount;

                            FFX2.FhCall.MsDamageBufferExe.fnptr!(chr_id, chr_id, pBuffer);//6422d0


                        }
                    }

                    if (sleep_expired2 != 0)
                    {
                    // Status timer decrementer?
                        FFX2.FhCall.MsSetStatus.fnptr!(chr_id, 0xff, 1, 1);//636ca0
                        FFX2.FhCall.MsSetChrWeak.fnptr!(chr_id, -1);//61b080
                    }

                    int iVar6;
                    if (sleep_expired1 == 0)
                    {
                        iVar6 = 0;
                    }
                    else
                    {
                        iVar6 = 2;
                        FFX2.FhCall.MsStatusEffectCheck.fnptr!((byte)chr_id);//623290
                    }
                    if (sleep_bit_cleared == 0)
                    {
                        if (iVar6 == 0) { return; }
                        ;
                    }
                    else
                    {
                        iVar6 = 0xc;
                    }
                    FFX2.FhCall.MsMotionRecoverExe.fnptr!(chr_id, iVar6);//6330e0


                }



            }
        }
    }

