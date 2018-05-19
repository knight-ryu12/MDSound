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
    enum EG_PARAM : int {
        eg_num_attack = 0,
        eg_num_decay = 1,
        eg_num_sustain = 2,
        eg_num_release = 3
    }
    public class ym3438 : Instrument
    {
        public override string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string ShortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private static void OPN2_DoIO(ym3438_ chip) {
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

        private static void OPN2_DoRegWrite(ym3438_ chip) {
            int i;
            Bit32u slot = chip.cycles % 12;
            Bit32u address;
            Bit32u channel = chip.channel;
            if (chip.write_fm_data) {
                if (ym3438_const.op_offset[slot] == (chip.address & 0x107)) {
                    if ((chip.address & 0x08) != 0) {
                        slot += 12; // OP2? OP4?
                    }
                    address = (Bit32u) chip.address & 0xf0;
                    switch (address)
                    {
                        case 0x30: //DT MULTI
                            chip.multi[slot] = (Bit8u) (chip.data & 0x0f);
                            if (chip.multi[slot] == 0) {
                                chip.multi[slot] = 1;
                            }
                            else {
                                chip.multi[slot] <<= 1;
                            }
                            chip.dt[slot] = (Bit8u) ((chip.data >> 4) & 0x07);
                            break;
                        case 0x40: //TL
                            chip.tl[slot] = (Bit8u)(chip.data & 0x7f);
                            break;
                        case 0x50: // KS AR
                            chip.ar[slot] = (Bit8u) (chip.data & 0x1f);
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

                if (ym3438_const.ch_offset[channel] == (chip.address & 0x103)) {
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
            if (chip.write_a_en || chip.write_d_en) {
                if (chip.write_a_en) { // True?
                    chip.write_fm_data = false;
                }
                if(chip.write_fm_address && chip.write_d_en)
                {
                    chip.write_fm_data = true;
                }

                if (chip.write_a_en) {
                    if ((chip.write_data & 0xf0) != 0x00) {
                        chip.address = chip.write_data;
                        chip.write_fm_address = true;
                    } else
                    {
                        chip.write_fm_address = false;
                    }    
                }
                if(chip.write_d_en && (chip.write_data & 0x100) == 0)
                {
                    switch (chip.address)
                    {
                        case 0x21: /* LSI test 1 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_21[i] = (((chip.write_data >> i) & 0x01)!=0?true:false);
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
                            chip.timer_a_load = (Bit8u) (chip.write_data & 0x01);
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
                                chip.mode_test_2c[i] = ((chip.write_data >> i) & 0x01)==1?true:false;
                            }
                            chip.dacdata &= 0x1fe;
                            //Bit8u tmp = (Bit8u)(chip.mode_test_2c[3] ? 1 : 0);
                            chip.dacdata |= (Bit8u)(chip.mode_test_2c[3] ? 1 : 0);
                            //Bit8u tmp = chip.mode_test_2c[7] ? 0 : 1;
                            chip.eg_custom_timer = (Bit8u)((chip.mode_test_2c[7] ? 0 : 1) & (chip.mode_test_2c[6]?1:0)); //todo
                            break;
                        default:
                            break;
                    }
                }
                    if (chip.write_a_en)
                    {
                        
                            chip.write_fm_mode_a = ((chip.write_data & 0xff)==1?true:false);
                    }    
                }

                if (chip.write_fm_data)
                {
                    chip.data = (Bit8u)(chip.write_data & 0xff);
                }
            }

        public void OPN2_PhaseCalcIncrement(ym3438_ chip) {
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
            if ((lfo_l & 0x08) != 0) {
                lfo_l ^= 0x0f;
            }
            fm = (fnum_h >> ym3438_const.pg_lfo_sh1[pms,lfo_l]) + (fnum_h >> ym3438_const.pg_lfo_sh2[pms,lfo_l]);
            if (pms > 5) fm <<= pms - 5;
            fm >>= 2;
            if ((lfo & 0x10)!=0)
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
            if (dt_l!=0)
            {
                if (kcode > 0x1c)
                {
                    kcode = 0x1c;
                }
                block = kcode >> 2;
                note = kcode & 0x03;
                sum = (Bit8u)(block + 9 + ((dt_l == 3) | (dt_l & 0x02)));
                sum_h = (Bit8u)(sum >> 1);
                sum_l = (Bit8u)(sum & 0x01);
                detune = (Bit8u)(ym3438_const.pg_detune[(sum_l << 2) | note] >> (9 - sum_h));
            }
            if (dt & 0x04)
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

        }



        public override void Reset(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock)
        {
            throw new NotImplementedException();
        }

        public override uint Start(byte ChipID, uint clock, uint ClockValue, params object[] option)
        {
            throw new NotImplementedException();
        }

        public override void Stop(byte ChipID)
        {
            throw new NotImplementedException();
        }

        public override void Update(byte ChipID, int[][] outputs, int samples)
        {
            throw new NotImplementedException();
        }

        public override int Write(byte ChipID, int port, int adr, int data)
        {
            throw new NotImplementedException();
        }
    }
    

    

    public class ym3438_
    {
        public Bit32u cycles;
        public Bit32u channel;
        Bit16s mol, mor;
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
        Bit8u pin_test_in;
        Bit8u pin_irq;
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
        Bit8u pg_block;
        public Bit8u pg_kcode;
        Bit32u[] pg_inc;
        Bit32u[] pg_phase;
        Bit8u[] pg_reset;
        Bit32u pg_read;
        /* Envelope generator */
        Bit8u eg_cycle;
        Bit8u eg_cycle_stop;
        Bit8u eg_shift;
        Bit8u eg_shift_lock;
        Bit8u eg_timer_low_lock;
        Bit16u eg_timer;
        Bit8u eg_timer_inc;
        Bit16u eg_quotient;
        public Bit8u eg_custom_timer;
        Bit8u eg_rate;
        Bit8u eg_ksv;
        Bit8u eg_inc;
        Bit8u eg_ratemax;
        Bit8u[] eg_sl;
        Bit8u eg_lfo_am;
        Bit8u[] eg_tl;
        Bit8u[] eg_state;
        Bit16u[] eg_level;
        Bit16u[] eg_out;
        Bit8u[] eg_kon;
        Bit8u[] eg_kon_csm;
        Bit8u[] eg_kon_latch;
        Bit8u[] eg_csm_mode;
        Bit8u[] eg_ssg_enable;
        Bit8u[] eg_ssg_pgrst_latch;
        Bit8u[] eg_ssg_repeat_latch;
        Bit8u[] eg_ssg_hold_up_latch;
        Bit8u[] eg_ssg_dir;
        Bit8u[] eg_ssg_inv;
        Bit32u[] eg_read;
        Bit8u eg_read_inc;
        /* FM */
        Bit16s[,] fm_op1;
        Bit16s[] fm_op2;
        Bit16s[] fm_out;
        Bit16u[] fm_mod;
        /* Channel */
        Bit16s[] ch_acc;
        Bit16s[] ch_out;
        Bit16s ch_lock;
        Bit8u ch_lock_l;
        Bit8u ch_lock_r;
        Bit16s ch_read;
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
