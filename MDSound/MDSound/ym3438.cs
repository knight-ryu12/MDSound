using System;

//Created by Chromaryu.
//Based off YM3438 Shot-Die Reverse Nuked OPN2 Source.
// True = 1, False = 0


using Bit8u = System.Byte;
using Bit8 = System.Byte;
using Bit16u = System.UInt16;
using Bit32u = System.UInt32;
using Bit16 = System.Int16;
using Bit32 = System.Int32;
using Bit16s = System.Int16;


namespace MDSound
{
    enum EG_PARAM : int
    {
        eg_num_attack = 0,
        eg_num_decay = 1,
        eg_num_sustain = 2,
        eg_num_release = 3
    }
    public class ym3438 : Instrument
    {
        public override string Name { get { return "YM3438"; } set { } }
        public override string ShortName { get { return "OPN2cmos"; } set { } }

        private static void OPN2_DoIO(ym3438_ chip)
        {
            chip.write_a_en = (chip.write_a & 0x03) == 0x01;
            chip.write_d_en = (chip.write_d & 0x03) == 0x01;
            chip.write_a <<= 1;
            chip.write_d <<= 1;
            //BUSY Counter
            chip.busy = chip.write_busy;
            chip.write_busy_cnt += 1;
            bool cnt5 = chip.write_busy_cnt >> 5 != 0 ? true : false;
            chip.write_busy = (chip.write_busy && !cnt5) || chip.write_d_en;
            chip.write_busy_cnt &= 0x1f;
        }

        private static void OPN2_DoRegWrite(ym3438_ chip)
        {
            int i;
            Bit32u slot = chip.cycles % 12;
            Bit32u address;
            Bit32u channel = chip.channel;
            if (chip.write_fm_data)
            {
                if (ym3438_const.op_offset[slot] == (chip.address & 0x107))
                {
                    if ((chip.address & 0x08) != 0)
                    {
                        slot += 12; // OP2? OP4?
                    }
                    address = (Bit32u)chip.address & 0xf0;
                    switch (address)
                    {
                        case 0x30: //DT MULTI
                            chip.multi[slot] = (Bit8u)(chip.data & 0x0f);
                            if (chip.multi[slot] == 0)
                            {
                                chip.multi[slot] = 1;
                            }
                            else
                            {
                                chip.multi[slot] <<= 1;
                            }
                            chip.dt[slot] = (Bit8u)((chip.data >> 4) & 0x07);
                            break;
                        case 0x40: //TL
                            chip.tl[slot] = (Bit8u)(chip.data & 0x7f);
                            break;
                        case 0x50: // KS AR
                            chip.ar[slot] = (Bit8u)(chip.data & 0x1f);
                            chip.ks[slot] = (Bit8u)((chip.data >> 7) & 0x03);
                            break;
                        case 0x60: // AM DR
                            chip.dr[slot] = (Bit8u)(chip.data & 0x1f);
                            chip.am[slot] = (Bit8u)((chip.data >> 7) & 0x01);
                            break;
                        case 0x70: //SR
                            chip.sr[slot] = (Bit8u)(chip.data & 0x1f);
                            break;
                        case 0x80: //SL RR
                            chip.rr[slot] = (Bit8u)(chip.data & 0x0f);
                            chip.sl[slot] = (Bit8u)((chip.data >> 4) & 0x0f);
                            chip.sl[slot] |= (Bit8u)((chip.sl[slot] + 1) & 0x10);
                            break;
                        case 0x90:
                            chip.ssg_eg[slot] = (Bit8u)(chip.data & 0x0f);
                            break;
                        default:
                            break;
                    }
                }

                if (ym3438_const.ch_offset[channel] == (chip.address & 0x103))
                {
                    address = (Bit32u)(chip.address & 0xfc);
                    switch (address)
                    {
                        case 0xa0: //Fnum, Block, kcode
                            chip.fnum[channel] = (Bit8u)((chip.data & 0xff) | ((chip.reg_a4 & 0x07) << 8));
                            chip.block[channel] = (Bit8u)((chip.reg_a4 >> 3) & 0x07);
                            chip.kcode[channel] = (Bit8u)((chip.block[channel] << 2) | ym3438_const.fn_note[chip.fnum[channel] >> 7]);
                            break;
                        case 0xa4: // a4?
                            chip.reg_a4 = (Bit8u)(chip.data & 0xff);
                            break;
                        case 0xa8: // fnum, block, kcode 3ch
                            chip.fnum_3ch[channel] = (Bit8u)((chip.data & 0xff) | ((chip.reg_ac & 0x07) << 8));
                            chip.block_3ch[channel] = (Bit8u)((chip.reg_ac >> 3) & 0x07);
                            chip.kcode_3ch[channel] = (Bit8u)((chip.block_3ch[channel] << 2) | ym3438_const.fn_note[chip.fnum_3ch[channel] >> 7]);
                            break;
                        case 0xac: //ac?
                            chip.reg_ac = (Bit8u)(chip.data & 0xff);
                            break;
                        case 0xb0: // Connect FeedBack
                            chip.connect[channel] = (Bit8u)(chip.data & 0x07);
                            chip.fb[channel] = (Bit8u)((chip.data >> 3) & 0x07);
                            break;
                        case 0xb4: //Modulate Pan
                            chip.pms[channel] = (Bit8u)(chip.data & 0x07);
                            chip.ams[channel] = (Bit8u)((chip.data >> 4) & 0x03);
                            chip.pan_l[channel] = (Bit8u)((chip.data >> 7) & 0x01);
                            chip.pan_r[channel] = (Bit8u)((chip.data >> 6) & 0x01);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (chip.write_a_en || chip.write_d_en)
            {
                if (chip.write_a_en)
                { // True?
                    chip.write_fm_data = false;
                }
                if (chip.write_fm_address && chip.write_d_en)
                {
                    chip.write_fm_data = true;
                }

                if (chip.write_a_en)
                {
                    if ((chip.write_data & 0xf0) != 0x00)
                    {
                        chip.address = chip.write_data;
                        chip.write_fm_address = true;
                    }
                    else
                    {
                        chip.write_fm_address = false;
                    }
                }
                if (chip.write_d_en && (chip.write_data & 0x100) == 0)
                {
                    switch (chip.address)
                    {
                        case 0x21: /* LSI test 1 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_21[i] = (((chip.write_data >> i) & 0x01) != 0 ? true : false);
                            }
                            break;
                        case 0x22: /* LFO control */
                            if (((chip.write_data >> 3) & 0x01) != 0)
                            {
                                chip.lfo_en = 0x7f;
                            }
                            else
                            {
                                chip.lfo_en = 0;
                            }
                            chip.lfo_freq = (Bit8u)(chip.write_data & 0x07);
                            break;
                        case 0x24: /* Timer A */
                            chip.timer_a_reg &= 0x03;
                            chip.timer_a_reg |= (Bit8u)((chip.write_data & 0xff) << 2);
                            break;
                        case 0x25:
                            chip.timer_a_reg &= 0x3fc;
                            chip.timer_a_reg |= (Bit8u)(chip.write_data & 0x03);
                            break;
                        case 0x26: /* Timer B */
                            chip.timer_b_reg = (Bit8u)(chip.write_data & 0xff);
                            break;
                        case 0x27: /* CSM, Timer control */
                            chip.mode_ch3 = (Bit8u)((chip.write_data & 0xc0) >> 6);
                            chip.mode_csm = chip.mode_ch3 == 2;
                            chip.timer_a_load = (Bit8u)(chip.write_data & 0x01);
                            chip.timer_a_enable = (Bit8u)((chip.write_data >> 2) & 0x01);
                            chip.timer_a_reset = (Bit8u)((chip.write_data >> 4) & 0x01);
                            chip.timer_b_load = (Bit8u)((chip.write_data >> 1) & 0x01);
                            chip.timer_b_enable = (Bit8u)((chip.write_data >> 3) & 0x01);
                            chip.timer_b_reset = (Bit8u)((chip.write_data >> 5) & 0x01);
                            break;
                        case 0x28: /* Key on/off */
                            for (i = 0; i < 4; i++)
                            {
                                chip.mode_kon_operator[i] = (Bit8u)((chip.write_data >> (4 + i)) & 0x01);
                            }
                            if ((chip.write_data & 0x03) == 0x03)
                            {
                                /* Invalid address */
                                chip.mode_kon_channel = 0xff;
                            }
                            else
                            {
                                chip.mode_kon_channel = (Bit8u)((chip.write_data & 0x03) + ((chip.write_data >> 2) & 1) * 3);
                            }
                            break;
                        case 0x2a: /* DAC data */
                            chip.dacdata &= 0x01;
                            chip.dacdata |= (Bit8u)((chip.write_data ^ 0x80) << 1);
                            break;
                        case 0x2b: /* DAC enable */
                            chip.dacen = (Bit8u)(chip.write_data >> 7);
                            break;
                        case 0x2c: /* LSI test 2 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_2c[i] = ((chip.write_data >> i) & 0x01) == 1 ? true : false;
                            }
                            chip.dacdata &= 0x1fe;
                            //Bit8u tmp = (Bit8u)(chip.mode_test_2c[3] ? 1 : 0);
                            chip.dacdata |= (Bit8u)(chip.mode_test_2c[3] ? 1 : 0);
                            //Bit8u tmp = chip.mode_test_2c[7] ? 0 : 1;
                            chip.eg_custom_timer = (Bit8u)((chip.mode_test_2c[7] ? 0 : 1) & (chip.mode_test_2c[6] ? 1 : 0)); //todo
                            break;
                        default:
                            break;
                    }
                }
                if (chip.write_a_en)
                {

                    chip.write_fm_mode_a = ((chip.write_data & 0xff) == 1 ? true : false);
                }
            }

            if (chip.write_fm_data)
            {
                chip.data = (Bit8u)(chip.write_data & 0xff);
            }
        }

        public void OPN2_PhaseCalcIncrement(ym3438_ chip)
        {
            Bit32u chan = chip.channel;
            Bit32u slot = chip.cycles;
            Bit32u fnum = chip.pg_fnum;
            Bit32u fnum_h = fnum >> 4;
            Bit32u fm;
            Bit32u basefreq;
            Bit8u lfo = chip.lfo_pm;
            Bit8u lfo_l = (Bit8u)(lfo & 0x0f);
            Bit8u pms = chip.pms[chan];
            Bit8u dt = chip.dt[slot];
            Bit8u dt_l = (Bit8u)(dt & 0x03);
            Bit8u detune = 0;
            Bit8u block, note;
            Bit8u sum, sum_h, sum_l;
            Bit8u kcode = (Bit8u)(chip.pg_kcode);

            fnum <<= 1;
            if ((lfo_l & 0x08) != 0)
            {
                lfo_l ^= 0x0f;
            }
            fm = (fnum_h >> ym3438_const.pg_lfo_sh1[pms, lfo_l]) + (fnum_h >> ym3438_const.pg_lfo_sh2[pms, lfo_l]);
            if (pms > 5) fm <<= pms - 5;
            fm >>= 2;
            if ((lfo & 0x10) != 0)
            {
                fnum -= fm;
            }
            else
            {
                fnum += fm;
            }
            fnum &= 0xfff;

            basefreq = (fnum << chip.pg_block) >> 2;

            /* Apply detune */
            if (dt_l != 0)
            {
                if (kcode > 0x1c)
                {
                    kcode = 0x1c;
                }
                block = (Bit8u)(kcode >> 2);
                note = (Bit8u)(kcode & 0x03);
                sum = (Bit8u)(block + 9 + (((dt_l == 3) ? 1 : 0) | (dt_l & 0x02)));
                sum_h = (Bit8u)(sum >> 1);
                sum_l = (Bit8u)(sum & 0x01);
                detune = (Bit8u)(ym3438_const.pg_detune[(sum_l << 2) | note] >> (9 - sum_h));
            }
            if ((dt & 0x04) != 0)
            {
                basefreq -= detune;
            }
            else
            {
                basefreq += detune;
            }
            basefreq &= 0x1ffff;
            chip.pg_inc[slot] = (basefreq * chip.multi[slot]) >> 1;
            chip.pg_inc[slot] &= 0xfffff;


        }
    

        public void OPN2_PhaseGenerate(ym3438_ chip)
        {
            Bit32u slot;
            /* Mask increment */
            slot = (chip.cycles + 20) % 24;
            if (chip.pg_reset[slot] != 0)
            {
                chip.pg_inc[slot] = 0;
            }
            /* Phase step */
            slot = (chip.cycles + 19) % 24;
            chip.pg_phase[slot] += chip.pg_inc[slot];
            chip.pg_phase[slot] &= 0xfffff;
            if (chip.pg_reset[slot] != 0 || chip.mode_test_21[3])
            {
                chip.pg_phase[slot] = 0;
            }
        }


        void OPN2_EnvelopeADSR(ym3438_ chip)
        {
            Bit32u slot = (chip.cycles + 22) % 24;

            Bit8u nkon = chip.eg_kon_latch[slot];
            Bit8u okon = chip.eg_kon[slot];
            Bit8u kon_event;
            Bit8u koff_event;
            Bit8u eg_off;
            Bit16s level;
            Bit16s nextlevel = 0;
            Bit16s ssg_level;
            Bit8u nextstate = chip.eg_state[slot];
            Bit16s inc = 0;
            chip.eg_read[0] = chip.eg_read_inc;
            chip.eg_read_inc = chip.eg_inc > 0;

            /* Reset phase generator */
            chip.pg_reset[slot] = (nkon && !okon) || chip.eg_ssg_pgrst_latch[slot];

            /* KeyOn/Off */
            kon_event = (nkon && !okon) || (okon && chip.eg_ssg_repeat_latch[slot]);
            koff_event = okon && !nkon;

            ssg_level = level = (Bit16s)chip.eg_level[slot];

            if (chip.eg_ssg_inv[slot] != 0)
            {
                /* Inverse */
                ssg_level = 512 - level;
                ssg_level &= 0x3ff;
            }
            if (koff_event != 0)
            {
                level = ssg_level;
            }
            if (chip.eg_ssg_enable[slot] != 0)
            {
                eg_off = level >> 9;
            }
            else
            {
                eg_off = (level & 0x3f0) == 0x3f0;
            }
            nextlevel = level;
            if (kon_event != 0)
            {
                nextstate = EG_PARAM.eg_num_attack;
                /* Instant attack */
                if (chip.eg_ratemax)
                {
                    nextlevel = 0;
                }
                else if (chip.eg_state[slot] == EG_PARAM.eg_num_attack && level != 0 && chip.eg_inc && nkon)
                {
                    inc = (~level << chip.eg_inc) >> 5;
                }
            }
            else
            {
                switch (chip.eg_state[slot])
                {
                    case EG_PARAM.eg_num_attack:
                        if (level == 0)
                        {
                            nextstate = eg_num_decay;
                        }
                        else if (chip.eg_inc && !chip.eg_ratemax && nkon)
                        {
                            inc = (~level << chip.eg_inc) >> 5;
                        }
                        break;
                    case EG_PARAM.eg_num_decay:
                        if ((level >> 5) == chip.eg_sl[1])
                        {
                            nextstate = eg_num_sustain;
                        }
                        else if (!eg_off && chip.eg_inc)
                        {
                            inc = 1 << (chip.eg_inc - 1);
                            if (chip.eg_ssg_enable[slot])
                            {
                                inc <<= 2;
                            }
                        }
                        break;
                    case EG_PARAM.eg_num_sustain:
                    case EG_PARAM.eg_num_release:
                        if (!eg_off && chip.eg_inc)
                        {
                            inc = 1 << (chip.eg_inc - 1);
                            if (chip.eg_ssg_enable[slot])
                            {
                                inc <<= 2;
                            }
                        }
                        break;
                    default:
                        break;
                }
                if (!nkon)
                {
                    nextstate = eg_num_release;
                }
            }
            if (chip.eg_kon_csm[slot])
            {
                nextlevel |= chip.eg_tl[1] << 3;
            }

            /* Envelope off */
            if (!kon_event && !chip.eg_ssg_hold_up_latch[slot] && chip.eg_state[slot] != eg_num_attack && eg_off)
            {
                nextstate = eg_num_release;
                nextlevel = 0x3ff;
            }

            nextlevel += inc;

            chip.eg_kon[slot] = chip.eg_kon_latch[slot];
            chip.eg_level[slot] = (Bit16u)nextlevel & 0x3ff;
            chip.eg_state[slot] = nextstate;
        }

        void OPN2_Reset(ym3438_ chip)
        {
            Bit32u i;
            chip = new ym3438_();
            for (i = 0; i < 24; i++)
            {
                chip.eg_out[i] = 0x3ff;
                chip.eg_level[i] = 0x3ff;
                chip.eg_state[i] = (Bit8u) EG_PARAM.eg_num_release;
                chip.multi[i] = 1;
            }
            for (i = 0; i < 6; i++)
            {
                chip.pan_l[i] = 1;
                chip.pan_r[i] = 1;
            }
        }

        void OPN2_Clock(ym3438_ chip, Bit32u[] buffer)
        {
            Bit32u slot = chip.cycles;
            chip.lfo_inc = (Bit8)(chip.mode_test_21[1] ? 1 : 0);
            chip.pg_read >>= 1;
            chip.eg_read[1] >>= 1;
            chip.eg_cycle++;
            /* Lock envelope generator timer value */
            if (chip.cycles == 1 && chip.eg_quotient == 2)
            {
                if (chip.eg_cycle_stop != 0)
                {
                    chip.eg_shift_lock = 0;
                }
                else
                {
                    chip.eg_shift_lock = (Bit8u)(chip.eg_shift + 1);
                }
                chip.eg_timer_low_lock = (Bit8u)(chip.eg_timer & 0x03);
            }
            /* Cycle specific functions */
            switch (chip.cycles)
            {
                case 0:
                    chip.lfo_pm = (Bit8u)(chip.lfo_cnt >> 2);
                    if ((chip.lfo_cnt & 0x40) != 0)
                    {
                        chip.lfo_am = (Bit8u)(chip.lfo_cnt & 0x3f);
                    }
                    else
                    {
                        chip.lfo_am = (Bit8u)(chip.lfo_cnt ^ 0x3f);
                    }
                    chip.lfo_am <<= 1;
                    break;
                case 1:
                    chip.eg_quotient++;
                    chip.eg_quotient %= 3;
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = 1;
                    chip.eg_shift = 0;
                    chip.eg_timer_inc |= (Bit8u)(chip.eg_quotient >> 1);
                    chip.eg_timer = (Bit16u)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (Bit8u)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 2:
                    chip.pg_read = chip.pg_phase[21] & 0x3ff;
                    chip.eg_read[1] = chip.eg_out[0];
                    break;
                case 13:
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = 1;
                    chip.eg_shift = 0;
                    chip.eg_timer = (Bit16u)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (Bit8u)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 23:
                    chip.lfo_inc |= 1;
                    break;
            }
            chip.eg_timer &= (Bit16u)(~((chip.mode_test_21[5] ? 1 : 0) << chip.eg_cycle));
            if ((((chip.eg_timer >> chip.eg_cycle) | (chip.pin_test_in & chip.eg_custom_timer)) & chip.eg_cycle_stop) != 0)
            {
                chip.eg_shift = chip.eg_cycle;
                chip.eg_cycle_stop = 0;
            }

            OPN2_DoIO(chip);

            OPN2_DoTimerA(chip);
            OPN2_DoTimerB(chip);
            OPN2_KeyOn(chip);

            OPN2_ChOutput(chip);
            OPN2_ChGenerate(chip);

            OPN2_FMPrepare(chip);
            OPN2_FMGenerate(chip);

            OPN2_PhaseGenerate(chip);
            OPN2_PhaseCalcIncrement(chip);

            OPN2_EnvelopeADSR(chip);
            OPN2_EnvelopeGenerate(chip);
            OPN2_EnvelopeSSGEG(chip);
            OPN2_EnvelopePrepare(chip);

            /* Prepare fnum & block */
            if (chip.mode_ch3 != 0)
            {
                /* Channel 3 special mode */
                switch (slot)
                {
                    case 1: /* OP1 */
                        chip.pg_fnum = chip.fnum_3ch[1];
                        chip.pg_block = chip.block_3ch[1];
                        chip.pg_kcode = chip.kcode_3ch[1];
                        break;
                    case 7: /* OP3 */
                        chip.pg_fnum = chip.fnum_3ch[0];
                        chip.pg_block = chip.block_3ch[0];
                        chip.pg_kcode = chip.kcode_3ch[0];
                        break;
                    case 13: /* OP2 */
                        chip.pg_fnum = chip.fnum_3ch[2];
                        chip.pg_block = chip.block_3ch[2];
                        chip.pg_kcode = chip.kcode_3ch[2];
                        break;
                    case 19: /* OP4 */
                    default:
                        chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                        chip.pg_block = chip.block[(chip.channel + 1) % 6];
                        chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
                        break;
                }
            }
            else
            {
                chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                chip.pg_block = chip.block[(chip.channel + 1) % 6];
                chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
            }

            OPN2_UpdateLFO(chip);
            OPN2_DoRegWrite(chip);
            chip.cycles = (chip.cycles + 1) % 24;
            chip.channel = chip.cycles % 6;

            buffer[0] = (Bit16u)chip.mol;
            buffer[1] = (Bit16u)chip.mor;
        }

        void OPN2_Write(ym3438_ chip, Bit32u port, Bit8u data)
        {
            port &= 3;
            chip.write_data = (Bit16u)(((port << 7) & 0x100) | data);
            if ((port & 1) != 0)
            {
                /* Data */
                chip.write_d |= 1;
            }
            else
            {
                /* Address */
                chip.write_a |= 1;
            }
        }



        private ym3438_[] ym3438_ = new ym3438_[2] { new ym3438_(), new ym3438_() };
        private uint clock = 0;
        private uint clockValue = 0;
        private object[] option;
        private Bit32u[] buf = new Bit32u[2];

        public override void Reset(byte ChipID)
        {
            OPN2_Reset(ym3438_[ChipID]);
        }

        public override uint Start(byte ChipID, uint clock)
        {
            OPN2_Reset(ym3438_[ChipID]);
            this.clock = clock;
            return 0;
        }

        public override uint Start(byte ChipID, uint clock, uint clockValue, params object[] option)
        {
            OPN2_Reset(ym3438_[ChipID]);
            this.clock = clock;
            this.clockValue = clockValue;
            this.option = option;
            return 0;
        }

        public override void Stop(byte ChipID)
        {
            OPN2_Reset(ym3438_[ChipID]);
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                OPN2_Clock(ym3438_[ChipID], buf);
                outputs[0][i] = (Bit32)buf[0];
                outputs[1][i] = (Bit32)buf[1];
            }
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            OPN2_Write(ym3438_[ChipID], (Bit16u)(port << 1), (Bit8u)adr);
            OPN2_Write(ym3438_[ChipID], (Bit16u)((port << 1) + 1), (Bit8u)data);
            return 0;
        }
    }




    public class ym3438_
    {
        public Bit32u cycles;
        public Bit32u channel;
        public Bit16s mol, mor;
        /* IO */
        public Bit16u write_data;
        public Bit8u write_a;
        public Bit8u write_d;
        public bool write_a_en;
        public bool write_d_en;
        public bool write_busy;
        public Bit8u write_busy_cnt;
        public bool write_fm_address;
        public bool write_fm_data;
        public bool write_fm_mode_a;
        public Bit16u address;
        public Bit8u data;
        public Bit8u pin_test_in;
        public Bit8u pin_irq;
        public bool busy;
        /* LFO */
        public Bit8u lfo_en;
        public Bit8u lfo_freq;
        public Bit8u lfo_pm;
        public Bit8u lfo_am;
        public Bit8u lfo_cnt;
        public Bit8u lfo_inc;
        public Bit8u lfo_quotient;
        /* Phase generator */
        public Bit16u pg_fnum;
        public Bit8u pg_block;
        public Bit8u pg_kcode;
        public Bit32u[] pg_inc;
        public Bit32u[] pg_phase;
        public Bit8u[] pg_reset;
        public Bit32u pg_read;
        /* Envelope generator */
        public Bit8u eg_cycle;
        public Bit8u eg_cycle_stop;
        public Bit8u eg_shift;
        public Bit8u eg_shift_lock;
        public Bit8u eg_timer_low_lock;
        public Bit16u eg_timer;
        public Bit8u eg_timer_inc;
        public Bit16u eg_quotient;
        public Bit8u eg_custom_timer;
        public Bit8u eg_rate;
        public Bit8u eg_ksv;
        public Bit8u eg_inc;
        public Bit8u eg_ratemax;
        public Bit8u[] eg_sl;
        public Bit8u eg_lfo_am;
        public Bit8u[] eg_tl;
        public Bit8u[] eg_state;
        public Bit16u[] eg_level;
        public Bit16u[] eg_out;
        public Bit8u[] eg_kon;
        public Bit8u[] eg_kon_csm;
        public Bit8u[] eg_kon_latch;
        public Bit8u[] eg_csm_mode;
        public Bit8u[] eg_ssg_enable;
        public Bit8u[] eg_ssg_pgrst_latch;
        public Bit8u[] eg_ssg_repeat_latch;
        public Bit8u[] eg_ssg_hold_up_latch;
        public Bit8u[] eg_ssg_dir;
        public Bit8u[] eg_ssg_inv;
        public Bit32u[] eg_read;
        public Bit8u eg_read_inc;
        /* FM */
        public Bit16s[,] fm_op1;
        public Bit16s[] fm_op2;
        public Bit16s[] fm_out;
        public Bit16u[] fm_mod;
        /* Channel */
        public Bit16s[] ch_acc;
        public Bit16s[] ch_out;
        public Bit16s ch_lock;
        public Bit8u ch_lock_l;
        public Bit8u ch_lock_r;
        public Bit16s ch_read;
        /* Timer */
        public Bit16u timer_a_cnt;
        public Bit16u timer_a_reg;
        public Bit8u timer_a_load_lock;
        public Bit8u timer_a_load;
        public Bit16u timer_a_enable;
        public Bit8u timer_a_reset;
        public Bit8u timer_a_load_latch;
        public Bit8u timer_a_overflow_flag;
        public Bit8u timer_a_overflow;

        public Bit16u timer_b_cnt;
        public Bit8u timer_b_subcnt;
        public Bit16u timer_b_reg;
        public Bit8u timer_b_load_lock;
        public Bit8u timer_b_load;
        public Bit8u timer_b_enable;
        public Bit8u timer_b_reset;
        public Bit8u timer_b_load_latch;
        public Bit8u timer_b_overflow_flag;
        public Bit8u timer_b_overflow;

        /* Register set */
        public bool[] mode_test_21;
        public bool[] mode_test_2c;
        public Bit8u mode_ch3;
        public Bit8u mode_kon_channel;
        public Bit8u[] mode_kon_operator;
        public Bit8u[] mode_kon;
        public bool mode_csm;
        public Bit8u mode_kon_csm;
        public Bit8u dacen;
        public Bit16s dacdata;

        public Bit8u[] ks;
        public Bit8u[] ar;
        public Bit8u[] sr;
        public Bit8u[] dt;
        public Bit8u[] multi;
        public Bit8u[] sl;
        public Bit8u[] rr;
        public Bit8u[] dr;
        public Bit8u[] am;
        public Bit8u[] tl;
        public Bit8u[] ssg_eg;

        public Bit16u[] fnum;
        public Bit8u[] block;
        public Bit8u[] kcode;
        public Bit16u[] fnum_3ch;
        public Bit8u[] block_3ch;
        public Bit8u[] kcode_3ch;
        public Bit8u reg_a4;
        public Bit8u reg_ac;
        public Bit8u[] connect;
        public Bit8u[] fb;
        public Bit8u[] pan_l, pan_r;
        public Bit8u[] ams;
        public Bit8u[] pms;

    }
}