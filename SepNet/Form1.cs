using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SepNet
{
    
    public partial class Form1 : Form
    {
        //клас підмережі
        public class SubNet
        {
            public SubNet(long capacity, string address, string prefix, string range, string broadcast)
            {
                Capacity = capacity;
                Address = address;
                Prefix = prefix;
                Range = range;
                Broadcast = broadcast;
            }
            public long Capacity { get; set; }
            public string Address { get; set; }
            public string Prefix { get; set; }
            public string Range { get; set; }
            public string Broadcast { get; set; }
        }
        //
        //найближче значення, що є степенем двійки
        public static long Nearest(long i)
        {
            long res = 2;
            int k = 1;
            while (res <= i)
            {
                //1 << k - 2 у степені k
                res = 1 << k++;
            }
            return res;
        }
        //
        //лаконічне перетворення типів
        public static string ToS(int i)
        {
            return Convert.ToString(i);
        }
        public static int ToI(string s)
        {
            if (s.All(char.IsDigit) && s.Length != 0)
            {
                return Convert.ToInt16(s);
            }
            else return -1;
        }
        public static long ToL(string s)
        {
            if (s.All(char.IsDigit))
            {
                return Convert.ToInt64(s);
            }
            else return -1;
        }
        //
        //знаходження ІР-адреси підмережі
        private string NextAddress(SubNet subNet)
        {
            int[] addr = new int[4];
            string[] br = subNet.Broadcast.Split('.');
            //широкомовна адреса попередньої підмережі + 1
            addr[3] = ((ToI(br[3]) + 1) == 256) ? 0 : ToI(br[3]) + 1;
            addr[2] += (ToI(br[2]) + ((addr[3] == 0) ? 1 : 0));
            addr[2] = (addr[2] == 256) ? 0 : addr[2];
            addr[1] += (ToI(br[1]) + ((addr[2] == 0) ? 1 : 0));
            addr[1] = (addr[1] == 256) ? 0 : addr[1];
            addr[0] += (ToI(br[0]) + ((addr[1] == 0) ? 1 : 0));
            return IPToStr(addr);
        }
        //
        //знаходження префіксу маски підмережі
        private int NextMaskPref(long cap)
        {
            long r = 2;
            int i = 1;
            while (r <= cap)
            {
                r = 1 << i++;
            }
            //префікс маски підмережі
            i = 33 - i;
            return i;
        }
        //
        //знаходження маски підмережі
        private int[] NextMask(long cap)
        {
            int i = NextMaskPref(cap);
            int[] res = new int[4];

            if (i >= 24)
            {
                res[0] = 255;
                res[1] = 255;
                res[2] = 255;
                //значення октету: 256 - 2 у степені (32 - префікс маски) 
                res[3] = 256 - (1 << (32 - i));
            }
            else if (i >= 16)
            {
                res[0] = 255;
                res[1] = 255;
                res[2] = 256 - (1 << (24 - i));
                res[3] = (i <= 24) ? 0 : 256 - (1 << (32 - i));
            }
            else if (i >= 8)
            {
                res[0] = 255;
                res[1] = 256 - (1 << (16 - i));
                res[2] = (i <= 16) ? 0 : 256 - (1 << (24 - i));
                res[3] = (i <= 24) ? 0 : 256 - (1 << (32 - i));
            }
            else
            {
                res[0] = 256 - (1 << (8 - i));
                res[1] = 0;
                res[2] = 0;
                res[3] = 0;
            }
            return res;
        }
        //
        //знаходження діапазону ІР-адрес підмережі
        private string NextRange(string address, string  prefix)
        {
            int[] res = new int[8];
            string[] addr = address.Split('.');
            string[] pref = prefix.Split('.');
            res[0] = ToI(addr[0]);
            res[1] = ToI(addr[1]); 
            res[2] = ToI(addr[2]);
            res[3] = ToI(addr[3])+1;
            //відповідний октет початку діапазону + (255 - відповідний октет маски)
            res[4] = res[0] + 255 - ToI(pref[0]);
            res[5] = res[1] + 255 - ToI(pref[1]);
            res[6] = res[2] + 255 - ToI(pref[2]);
            res[7] = res[3] + 253 - ToI(pref[3]);
            return IPToStr(res);
        }
        //
        //знаходження широкомовної адреси підмережі
        private string NextBroadcast(string range)
        {
            int[] br = new int[4];
            string[] w = range.Split('.');
            //ІР-адреса кінця діапазону + 1
            var t = w[3].Split(new String[] { " - " }, StringSplitOptions.RemoveEmptyEntries)[1];
            br[0] = ToI(t);
            br[1] = ToI(w[4]);
            br[2] = ToI(w[5]);
            br[3] = ToI(w[6])+1;
            return IPToStr(br);
        }
        //
        //визначення розміру підмереж при рівній розмірності
        private long[] EqualCapacities(int count, long cap)
        {
            long[] caps = new long[count];
            int k = 2;
            while ((1 << k) * count < cap)
            {
                k++;
            }
            k--;
            if ((1 << k) - 2 < 1)
            {
                MessageBox.Show("Неможливо розбити мережу на таку кількість підмереж", "Помилка");
                return caps;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    caps[i] = (1 << k) - 2;
                }
                return caps;
            }
        }
        //
        //перетворення ІР-адреси у рядок
        private string IPToStr(int[] v)
        {
            string s = "";
            if (v.Length == 4)
            {
                s = String.Join(".", v);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    s += v[i] + ".";
                }
                s += v[3] + " - ";
                for (int i = 4; i < 8; i++)
                {
                    s += v[i] + ".";
                }
                s = s.Remove(s.Length - 1, 1);
            }
            return s;
        }

        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if ((ToI(t1.Text)>=0 && ToI(t1.Text) < 256 && ToI(t2.Text) >= 0 && ToI(t2.Text) < 256 && ToI(t3.Text) >= 0 && ToI(t3.Text) < 256 && ToI(t4.Text) >= 0 && ToI(t4.Text) < 255)
                && (ToI(t1.Text) + ToI(t2.Text) + ToI(t3.Text) + ToI(t4.Text) > 0)) {
                //визначення ІР-адреси мережі, використовуючи операцію логічного "i"(&)
                t9.Text = ToS(ToI(t1.Text) & ToI(t5.Text));
                t10.Text = ToS(ToI(t2.Text) & ToI(t6.Text));
                t11.Text = ToS(ToI(t3.Text) & ToI(t7.Text));
                t12.Text = ToS(ToI(t4.Text) & ToI(t8.Text));
                //..........................................
                //визначення ІР-адреси мережевого хосту
                int d1 = ToI(t1.Text) - ToI(t9.Text);
                int d2 = ToI(t2.Text) - ToI(t10.Text);
                int d3 = ToI(t3.Text) - ToI(t11.Text);
                int d4 = ToI(t4.Text) - ToI(t12.Text);
                t21.Text = (d1 > 0) ? ToS(d1) : "0";
                t22.Text = (d2 > 0) ? ToS(d2) : "0";
                t23.Text = (d3 > 0) ? ToS(d3) : "0";
                t24.Text = (d4 > 0) ? ToS(d4) : "0";
                //.................................
                //визначення ІР-адрес початку та кінця діапазону ІР-адрес мережі 
                t13.Text = t9.Text;
                t14.Text = t10.Text;
                t15.Text = t11.Text;
                t16.Text = ToS(ToI(t12.Text) + 1);
                t17.Text = ToS(ToI(t13.Text) + 255 - ToI(t5.Text));
                t18.Text = ToS(ToI(t14.Text) + 255 - ToI(t6.Text));
                t19.Text = ToS(ToI(t15.Text) + 255 - ToI(t7.Text));
                t20.Text = ToS(ToI(t16.Text) + 253 - ToI(t8.Text));
                //.................................................
                //визначення широкомовної адреси мережі
                t25.Text = t17.Text;
                t26.Text = t18.Text;
                t27.Text = t19.Text;
                t28.Text = ToS(ToI(t20.Text) + 1);
                //................................
                capacityTB.Text = ToS((1 << (32 - (int)nUD.Value)) - 2);
                button2.Enabled = true;
            }
            else
            {
                MessageBox.Show("Недопустимий формат ІР-адреси", "Помилка");
            }

        }
        //
        //при зміні значення лічильника маски змінюється значення ІР-адреси маски
        private void nUD_ValueChanged(object sender, EventArgs e)
        {
            int[] res = new int[4];
            if (nUD.Value >= 24)
            {
                res[0] = 255;
                res[1] = 255;
                res[2] = 255;
                res[3] = 256 - (1 << (32 - (int)nUD.Value));
            }
            else if (nUD.Value >= 16)
            {
                res[0] = 255;
                res[1] = 255;
                res[2] = 256 - (1 << (24 - (int)nUD.Value));
                res[3] = (nUD.Value <= 24) ? 0 : 256 - (1 << (32 - (int)nUD.Value));
            }
            else if (nUD.Value >= 8)
            {
                res[0] = 255;
                res[1] = 256 - (1 << (16 - (int)nUD.Value));
                res[2] = (nUD.Value <= 16) ? 0 : 256 - (1 << (24 - (int)nUD.Value));
                res[3] = (nUD.Value <= 24) ? 0 : 256 - (1 << (32 - (int)nUD.Value));
            }
            else
            {
                res[0] = 256 - (1 << (8 - (int)nUD.Value));
                res[1] = 0;
                res[2] = 0;
                res[3] = 0;
            }
            t5.Text = ToS(res[0]);
            t6.Text = ToS(res[1]);
            t7.Text = ToS(res[2]);
            t8.Text = ToS(res[3]);
        }
        //
        //формування вихідних даних про підмережі
        private void button2_Click(object sender, EventArgs e)
        {
            //створення списку підмереж
            List<SubNet> SubNets = new List<SubNet>();
            long sum = 0;
            string[] words = subNets.Text.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            long[] capacities = new long[(int)nSUD.Value];
            if (words.Length == 0)
            {
                capacities = EqualCapacities((int)nSUD.Value, ToL(capacityTB.Text));
                if (capacities.Length == 0)
                {
                    MessageBox.Show("Не вдається розбити мережу на таку кількість підмереж", "Помилка");
                }
            }
            else
            {
                for (int i = 0; i < words.Length; i++)
                {
                    capacities[i] = ToL(words[i]);
                    if (capacities[i] < 2)
                    {
                        MessageBox.Show("Неприпустимий розмір підмережі", "Помилка");
                    }
                    else
                        sum += Nearest(capacities[i]);
                }
            }
            if (words.Length != nSUD.Value && words.Length != 0)
            {
                MessageBox.Show("Отримані дані про підмережі не збігаються", "Помилка");
            } 
            else if (sum > Convert.ToInt64(capacityTB.Text))
            {
                MessageBox.Show("Дана мережа не вміщує таку кількість хостів", "Помилка");
            }
            else if(capacities[0]!=0)
            {
                //сортування масиву із вказаними попередньо розмірами підмереж
                Array.Sort(capacities, (a, b) => b.CompareTo(a));
                int[] addr = new int[] { ToI(t9.Text), ToI(t10.Text), ToI(t11.Text), ToI(t12.Text) };
                int[] pref = NextMask(capacities[0]);
                int[] r = new int[] { addr[0], addr[1], addr[2], addr[3]+1, addr[0]+255-pref[0], addr[1] + 255 - pref[1], addr[2] + 255 - pref[2], addr[3] + 254 - pref[3] };
                int[] br = new int[] { r[4], r[5], r[6], r[7] + 1 };
                //першим елементом списку буде підмережа найбільшого розміру, що починається з ІР-адреси мережі 
                SubNets.Add(new SubNet(capacities[0], IPToStr(addr), IPToStr(pref) + " (/" + ToS(NextMaskPref(capacities[0]))+")", IPToStr(r), IPToStr(br)));
                //цикл для додавання усіх підмереж до списку
                for (int i = 1; i < capacities.Length; i++)
                {
                    string address = NextAddress(SubNets[i - 1]);
                    string prefix = IPToStr(NextMask(capacities[i]));
                    string range = NextRange(address,prefix);
                    string broadcast = NextBroadcast(range);
                    SubNets.Add(new SubNet(capacities[i], address, prefix + " (/" + ToS(NextMaskPref(capacities[i])) + ")", range, broadcast));
                }
                //формування таблиці з результатом розбиття
                var source = new BindingSource(SubNets, null);
                SubNetGridView.DataSource = source;
            }
        }
    }
}
