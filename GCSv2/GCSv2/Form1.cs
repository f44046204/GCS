﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Collections.Generic;
using System.Data;

namespace GCSv2
{
    public partial class Form1 : Form
    {
        /*模擬用參數*/
        public double yawRate = 50.0;
        public double avgSpd = 10.0;

        /*無人機地圖顯示*/
        public List<GMapOverlay> planes = new List<GMapOverlay>();
        public List<GMapOverlay> markers = new List<GMapOverlay>();
        public List<GMapRoute> routes = new List<GMapRoute>();
        public List<List<PointLatLng>> points = new List<List<PointLatLng>>();
        /*模擬顯示*/
        public List<PointLatLng> simLLH = new List<PointLatLng>();
        public List<double[]> simXYZ = new List<double[]>();
        public List<double[]> simV = new List<double[]>();
        public List<double> yaw = new List<double>();
        public List<int> wp = new List<int>();
        Random random = new Random();
        /*飛行資訊*/
        public List<DataTable> dt = new List<DataTable>();
        public List<DataGridView> dataGridViews = new List<DataGridView>();
        public List<Label> labels = new List<Label>();
        public List<UdpClient> udpClients = new List<UdpClient>();
        public List<byte[]> UdpDatas = new List<byte[]>();
        public List<int> ports = new List<int>();
        public List<Buffer> buffers = new List<Buffer>();

        public Form1()
        {
            InitializeComponent();
        }

        private void gMapControl1_Load(object sender, EventArgs e)
        {
            /* GMap 初始參數 */
            var iniPosition = new PointLatLng(22.995620, 120.223111);
            gMapControl1.DragButton = MouseButtons.Left;
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position = iniPosition;
            gMapControl1.MaxZoom = 24;
            gMapControl1.MinZoom = 3;
            gMapControl1.Zoom = 20;
            gMapControl1.ShowCenter = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = MessageBox.Show(this, "確定退出？", "退出視窗通知", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var udpReceiveUdpThread = new Thread(ThreadRunMethod);

            if (!checkBox1.Checked)
            {
                udpReceiveUdpThread.Start();

                timer1.Enabled = true;
                timer1.Start();
                timer1.Interval = 1000;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Stop();
            dataGridView1.Rows.Clear();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int n = 0; n < udpClients.Count; n++)
            {
                double V = Math.Sqrt(Math.Pow(buffers[n].vx, 2) + Math.Pow(buffers[n].vy, 2));
                V = double.Parse(V.ToString("F2"));

                Planes.AddPosition(buffers[n].lat, buffers[n].lng, planes[n].Id, buffers[n].heading, V, planes[n]);

                if (planes[n].Markers.Count > 1)
                {
                    Image srcRoute = Image.FromFile("dot_red.png");
                    Bitmap picRoute = (Bitmap)srcRoute;

                    for (int i = 0; i < planes[n].Markers.Count - 1; i++)
                    {
                        GMapMarker route = new GMarkerGoogle(new PointLatLng(planes[n].Markers[i].Position.Lat, planes[n].Markers[i].Position.Lng), picRoute);
                        planes[n].Markers.RemoveAt(i);
                        planes[n].Markers.Insert(i, route);
                    }

                    if (planes[n].Markers.Count > 51)
                    {
                        for (int i = 0; i < planes[n].Markers.Count - 51; i++)
                            planes[n].Markers[i].IsVisible = false;
                    }
                }

                if (dataGridView1.Rows.Count <= planes.Count)
                    dataGridView1.Rows.Add(planes[n].Id, buffers[n].lat, buffers[n].lng, buffers[n].height, buffers[n].heading, V);
                else
                {
                    dataGridView1.Rows.RemoveAt(n);
                    dataGridView1.Rows.Insert(n, planes[n].Id, buffers[n].lat, buffers[n].lng, buffers[n].height, buffers[n].heading, V);
                }
            }
        }

        private void gMapControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button==MouseButtons.Right)
            {
                for (int n = 0;n < planes.Count; n++)
                {
                    if (comboBox1.Text == planes[n].Id)
                    {
                        PointLatLng point = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                        
                        Bitmap pin = new Bitmap(21, 21);
                        for (int x = 0; x < pin.Width; x++)
                        {
                            for (int y = 0; y < pin.Height; y++)
                            {
                                pin.SetPixel(x, y, Color.FromArgb(0, Math.Min(((n % 4 * 51) + 100), 255), Math.Min(((n % 4 * 51) + 100), 255)));
                            }
                        }
                        
                        GMapMarker marker = new GMarkerGoogle(point, pin);
                        marker.ToolTipText = Convert.ToString(markers[n].Markers.Count + 1);
                        marker.ToolTip.Fill = Brushes.Transparent;
                        marker.ToolTip.Offset = new Point(marker.LocalPosition.X - 18, marker.LocalPosition.Y - 15);
                        marker.ToolTip.Stroke.Color = Color.Transparent;
                        marker.ToolTip.Foreground = Brushes.Black;
                        marker.ToolTip.Font = new Font("Arial", 10);
                        marker.ToolTipMode = MarkerTooltipMode.Always;
                        markers[n].Markers.Add(marker);
                        points[n].Add(point);

                        if (markers[n].Markers.Count > 1)
                        {
                            routes[n] = new GMapRoute(points[n], routes[n].Name);
                            markers[n].Routes.Add(routes[n]);
                        }

                        dt[n].Rows.Add(new Object[] { markers[n].Markers.Count, (int)(point.Lat * 10000000), (int)(point.Lng * 10000000), 30, 0 });
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            for (int n = 0; n < planes.Count; n++)
            {
                if (comboBox1.Text == planes[n].Id)
                {
                    markers[n].Clear();
                    points[n].Clear();
                    dt[n].Clear();
                    wp[n] = 0;
                }
            }
        }
        
        //模擬用計時器
        private void timer2_Tick(object sender, EventArgs e)
        {
            for (int n = 0; n < planes.Count; n++)
            {
                if (markers[n].Markers.Count > 0)
                {
                    double[] wpXYZ = new double[3];
                    wpXYZ = CoordinateTransform.llh2xyz(markers[n].Markers[wp[n]].Position.Lat, markers[n].Markers[wp[n]].Position.Lng, 30);
                    simXYZ[n] = CoordinateTransform.llh2xyz(simLLH[n].Lat, simLLH[n].Lng, 30);
                    double wpAngle = Math.Atan((markers[n].Markers[wp[n]].Position.Lng - simLLH[n].Lng)
                                              /(markers[n].Markers[wp[n]].Position.Lat - simLLH[n].Lat)) * 180 / Math.PI;
                    double plane2wp = Math.Sqrt(Math.Pow(wpXYZ[0] - simXYZ[n][0], 2) 
                                              + Math.Pow(wpXYZ[1] - simXYZ[n][1], 2) 
                                              + Math.Pow(wpXYZ[2] - simXYZ[n][2], 2));

                    simV[n][0] = (wpXYZ[0] - simXYZ[n][0]) * avgSpd / plane2wp;
                    simV[n][1] = (wpXYZ[1] - simXYZ[n][1]) * avgSpd / plane2wp;
                    simV[n][2] = (wpXYZ[2] - simXYZ[n][2]) * avgSpd / plane2wp;
                    
                    if (markers[n].Markers[wp[n]].Position.Lat - simLLH[n].Lat < 0)
                        wpAngle += 180;

                    //限制角度在0-360
                    wpAngle = TCAS.angleLimit(wpAngle);
                    yaw[n] = TCAS.angleLimit(yaw[n]);

                    double desireYaw = 0;
                    desireYaw = wpAngle - yaw[n];
                    desireYaw = TCAS.angleLimit(desireYaw);

                    //Console.WriteLine(wpAngle);

                    if (desireYaw <= 180)   //轉向
                    {
                        if (desireYaw > yawRate)
                            yaw[n] += yawRate;
                        else
                            yaw[n] = wpAngle;
                    }
                    else
                    {
                        if (Math.Abs(360 - desireYaw) > yawRate)
                            yaw[n] -= yawRate;
                        else
                            yaw[n] = wpAngle;
                    }
                    /*
                    if (yaw[n] == wpAngle)     //速度
                    {
                        simV[n][0] = avgSpd * Math.Sin((450 - yaw[n]) * Math.PI / 180);
                        simV[n][1] = avgSpd * Math.Cos((450 - yaw[n]) * Math.PI / 180);
                        simV[n][2] = 0;
                    }*/

                    //double dis = Math.Sqrt(Math.Pow(wpNED[0] - simNED[n][0], 2) + Math.Pow(wpNED[1] - simNED[n][1], 2));
                    //Console.WriteLine("distance:" + dis);
                    if (plane2wp < 10)
                    {
                        if (wp[n] < markers[n].Markers.Count - 1)
                            wp[n] += 1;
                        else
                        {
                            for (int i = 0; i < 3; i++)
                                simV[n][i] = 0;
                        }
                    }
                    //label5.Text = "Heading to No. " + (wp1 + 1);

                    simXYZ[n][0] += simV[n][0];
                    simXYZ[n][1] += simV[n][1];
                    simXYZ[n][2] += simV[n][2];
                    
                    double[] LLH = CoordinateTransform.xyz2llh(simXYZ[n][0], simXYZ[n][1], simXYZ[n][2]);
                    PointLatLng latLng = new PointLatLng();
                    latLng.Lat = LLH[0];
                    latLng.Lng = LLH[1];
                    simLLH[n] = latLng;

                    for (int i = 0; i < planes[n].Markers.Count; i++)
                        planes[n].Markers[i].IsVisible = false;
                    Planes.AddPosition(simLLH[n].Lat, simLLH[n].Lng, planes[n].Id, yaw[n], avgSpd, planes[n]);
                    //TCAS.TAbubble(simLLH[n].Lat, simLLH[n].Lng, avgSpd, (int)gMapControl1.Zoom, planes[n]);
                    
                    labels[n].Text = planes[n].Id + ": distance to waypoint " + (wp[n]+1) + " is " + (int)plane2wp;
                }
                if (dataGridView1.Rows.Count <= planes.Count)
                    dataGridView1.Rows.Add(new Object[] { planes[n].Id, simLLH[n].Lat, simLLH[n].Lng, 30, (int)yaw[n], avgSpd });
                else
                {
                    dataGridView1.Rows.RemoveAt(n);
                    dataGridView1.Rows.Insert(n, new Object[] { planes[n].Id, simLLH[n].Lat, simLLH[n].Lng, 30, (int)yaw[n], avgSpd });
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Start Mission")
            {
                if(checkBox1.Checked)
                {
                    timer2.Enabled = true;
                    timer2.Start();
                    timer2.Interval = 1000;
                    button3.Text = "Stop Mission";
                }
            }
            else
            {
                if(checkBox1.Checked)
                {
                    timer2.Enabled = false;
                    timer2.Stop();
                    dataGridView1.Rows.Clear();
                    button3.Text = "Start Mission";
                }
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.ReadOnly = true;

            DialogResult result = MessageBox.Show("Add new UAV to the GCS?", "new UAV", MessageBoxButtons.OKCancel);
            if (result == DialogResult.OK)
            {
                if (!checkBox1.Checked)
                {
                    int port;
                    port = int.Parse(textBox2.Text);
                    if (!ports.Contains(port))
                    {
                        ports.Add(port);
                        udpClients.Add(new UdpClient(port));
                        UdpDatas.Add(new byte[1024]);
                        buffers.Add(new Buffer());
                        planes.Add(new GMapOverlay(Name = textBox1.Text));
                        gMapControl1.Overlays.Add(planes[planes.Count - 1]);
                        markers.Add(new GMapOverlay(Name = "markers" + planes.Count));
                        gMapControl1.Overlays.Add(markers[markers.Count - 1]);
                        dt.Add(new DataTable());
                        routes.Add(new GMapRoute(Name = "routes" + planes.Count));
                        points.Add(new List<PointLatLng>());
                        tabControl1.TabPages.Add(planes[planes.Count - 1].Id);
                        dataGridViews.Add(new DataGridView());
                        labels.Add(new Label());
                        comboBox1.Items.Add(textBox1.Text);
                        textBox1.Text = "UAV" + (planes.Count + 1);

                        //每台飛機的導航點清單
                        dataGridViews[dataGridViews.Count - 1].DataSource = dt[dt.Count - 1];
                        dt[dt.Count - 1].Columns.Add("Sequence", typeof(int));
                        dt[dt.Count - 1].Columns.Add("Lat", typeof(int));
                        dt[dt.Count - 1].Columns.Add("Lng", typeof(int));
                        dt[dt.Count - 1].Columns.Add("Alt", typeof(int));
                        dt[dt.Count - 1].Columns.Add("Time", typeof(int));
                        dataGridViews[dataGridViews.Count - 1].Visible = true;
                        dataGridViews[dataGridViews.Count - 1].Parent = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                        dataGridViews[dataGridViews.Count - 1].Location = new Point(0, 0);
                        dataGridViews[dataGridViews.Count - 1].Width = tabControl1.Width;
                        dataGridViews[dataGridViews.Count - 1].Height = tabControl1.Height;
                        dataGridViews[dataGridViews.Count - 1].BackgroundColor = Color.White;

                        labels[labels.Count - 1].Visible = true;
                        labels[labels.Count - 1].Text = planes[planes.Count - 1].Id + ":";
                        labels[labels.Count - 1].Parent = tabControl2.TabPages[0];
                        labels[labels.Count - 1].Location = new Point(0, (planes.Count - 1) * 20);
                        labels[labels.Count - 1].BackColor = Color.Transparent;
                        labels[labels.Count - 1].BringToFront();
                        labels[labels.Count - 1].Width = 200;
                    }
                    else
                        MessageBox.Show("UDPport already exists.");
                }
                else
                {
                    planes.Add(new GMapOverlay(Name = textBox1.Text));
                    gMapControl1.Overlays.Add(planes[planes.Count - 1]);
                    double randomLat = 22.0 + (random.Next(906277, 1185923) / 1000000.0);
                    double randomLng = 120.0 + (random.Next(32532, 307118) / 1000000.0);
                    Planes.AddPosition(randomLat, randomLng, textBox1.Text, 0, avgSpd, planes[planes.Count - 1]);
                    markers.Add(new GMapOverlay(Name = "markers" + planes.Count));
                    gMapControl1.Overlays.Add(markers[markers.Count - 1]);
                    dt.Add(new DataTable());
                    routes.Add(new GMapRoute(Name = "routes" + planes.Count));
                    points.Add(new List<PointLatLng>());
                    simLLH.Add(new PointLatLng());
                    simLLH[simLLH.Count - 1] = new PointLatLng(randomLat, randomLng);
                    simXYZ.Add(new double[3]);
                    simV.Add(new double[3]);
                    yaw.Add(0.0);
                    wp.Add(0);
                    tabControl1.TabPages.Add(planes[planes.Count - 1].Id);
                    dataGridViews.Add(new DataGridView());
                    labels.Add(new Label());
                    comboBox1.Items.Add(textBox1.Text);
                    textBox1.Text = "UAV" + (planes.Count + 1);

                    //每台飛機的導航點清單
                    dataGridViews[dataGridViews.Count - 1].DataSource = dt[dt.Count - 1];
                    dt[dt.Count - 1].Columns.Add("Sequence", typeof(int));
                    dt[dt.Count - 1].Columns.Add("Lat", typeof(int));
                    dt[dt.Count - 1].Columns.Add("Lng", typeof(int));
                    dt[dt.Count - 1].Columns.Add("Alt", typeof(int));
                    dt[dt.Count - 1].Columns.Add("Time", typeof(int));
                    dataGridViews[dataGridViews.Count - 1].Visible = true;
                    dataGridViews[dataGridViews.Count - 1].Parent = tabControl1.TabPages[tabControl1.TabPages.Count - 1];
                    dataGridViews[dataGridViews.Count - 1].Location = new Point(0, 0);
                    dataGridViews[dataGridViews.Count - 1].Width = tabControl1.Width;
                    dataGridViews[dataGridViews.Count - 1].Height = tabControl1.Height;
                    dataGridViews[dataGridViews.Count - 1].BackgroundColor = Color.White;

                    labels[labels.Count - 1].Visible = true;
                    labels[labels.Count - 1].Text = planes[planes.Count - 1].Id + ":";
                    labels[labels.Count - 1].Parent = tabControl2.TabPages[0];
                    labels[labels.Count - 1].Location = new Point(0, (planes.Count - 1) * 20);
                    labels[labels.Count - 1].BackColor = Color.Transparent;
                    labels[labels.Count - 1].BringToFront();
                    labels[labels.Count - 1].Width = 200;
                }
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                if (e.KeyChar == 13)
                    button5.PerformClick();
                else
                    MessageBox.Show("Please enter a valid number.");
            }
        }

        public class Buffer
        {
            public double lat, lng, heading, height;
            public double time;
            public double vx, vy;
            public string name;
        }

        //接收GPS資訊
        public void ThreadRunMethod()
        {
            var remoteIp = new IPEndPoint(IPAddress.Any, 0);
            
            while (true)
            {
                for (int n = 0; n < udpClients.Count; n++)
                {
                    udpClients[n].Client.ReceiveBufferSize = 4096;
                    UdpDatas[n] = udpClients[n].Receive(ref remoteIp);

                    if(UdpDatas[n][5] == 33)
                    {
                        buffers[n].time = BitConverter.ToUInt32(UdpDatas[n], 6);
                        buffers[n].lat = BitConverter.ToInt32(UdpDatas[n], 10) / 10000000.0;
                        buffers[n].lng = BitConverter.ToInt32(UdpDatas[n], 14) / 10000000.0;
                        buffers[n].height = BitConverter.ToInt32(UdpDatas[n], 18) / 1000.0;
                        buffers[n].vx = BitConverter.ToInt16(UdpDatas[n], 26) / 100.0;
                        buffers[n].vy = BitConverter.ToInt16(UdpDatas[n], 28) / 100.0;
                        buffers[n].heading = BitConverter.ToUInt16(UdpDatas[n], 32) / 100.0;
                    }
                }
                /*
                udpClient1.Client.ReceiveBufferSize = 4096;
                udpClient2.Client.ReceiveBufferSize = 4096;
                udpClient3.Client.ReceiveBufferSize = 4096;

                UdpData1 = udpClient1.Receive(ref remoteIp);
                //UdpData2 = udpClient2.Receive(ref remoteIp);
                //UdpData3 = udpClient3.Receive(ref remoteIp);
                
                if (UdpData1[5] == 33)
                {
                    buffer1.time = BitConverter.ToUInt32(UdpData1, 6);
                    buffer1.lat = BitConverter.ToInt32(UdpData1, 10) / 10000000.0;
                    buffer1.lng = BitConverter.ToInt32(UdpData1, 14) / 10000000.0;
                    buffer1.height = BitConverter.ToInt32(UdpData1, 18) / 1000.0;
                    buffer1.vx = BitConverter.ToInt16(UdpData1, 26) / 100.0;
                    buffer1.vy = BitConverter.ToInt16(UdpData1, 28) / 100.0;
                    buffer1.heading = BitConverter.ToUInt16(UdpData1, 32) / 100.0;
                }

                if (UdpData2[5] == 33)
                {
                    buffer2.time = BitConverter.ToUInt32(UdpData2, 6);
                    buffer2.lat = BitConverter.ToInt32(UdpData2, 10) / 10000000.0;
                    buffer2.lng = BitConverter.ToInt32(UdpData2, 14) / 10000000.0;
                    buffer2.height = BitConverter.ToInt32(UdpData2, 18) / 1000.0;
                    buffer2.vx = BitConverter.ToInt16(UdpData2, 26) / 100.0;
                    buffer2.vy = BitConverter.ToInt16(UdpData2, 28) / 100.0;
                    buffer2.heading = BitConverter.ToUInt16(UdpData2, 32) / 100.0;
                }

                if (UdpData3[5] == 33)
                {
                    buffer3.time = BitConverter.ToUInt32(UdpData3, 6);
                    buffer3.lat = BitConverter.ToInt32(UdpData3, 10) / 10000000.0;
                    buffer3.lng = BitConverter.ToInt32(UdpData3, 14) / 10000000.0;
                    buffer3.height = BitConverter.ToInt32(UdpData3, 18) / 1000.0;
                    buffer3.vx = BitConverter.ToInt16(UdpData3, 26) / 100.0;
                    buffer3.vy = BitConverter.ToInt16(UdpData3, 28) / 100.0;
                    buffer3.heading = BitConverter.ToUInt16(UdpData3, 32) / 100.0;
                }
                */
                Thread.Sleep(10);
            }
        }
    }
}
