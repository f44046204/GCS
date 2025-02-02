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
using System.Windows.Media.Media3D;

namespace GCSv2
{
    public partial class Form1 : Form
    {
        Record record = new Record();

        /*模擬用參數*/
        public double yawRate = 50.0;
        public double avgSpd = 10.0;
        public double topSpd = 30.0;

        /*無人機地圖顯示*/
        public List<GMapOverlay> planes = new List<GMapOverlay>();
        public List<GMapOverlay> markers = new List<GMapOverlay>();
        public List<GMapRoute> routes = new List<GMapRoute>();
        public List<List<PointLatLng>> points = new List<List<PointLatLng>>();
        /*碰撞預警*/
        public GMapOverlay Warning = new GMapOverlay();
        public List<bool> isWarning = new List<bool>();
        public List<double> distance2UAV = new List<double>();
        /*模擬顯示*/
        public List<PointLatLng> simLLH = new List<PointLatLng>();
        public List<double> yaw = new List<double>();
        public List<int> wp = new List<int>();
        Random random = new Random();
        public List<bool> isReached = new List<bool>();
        public List<Point3D> simXYZ = new List<Point3D>();
        public List<Vector3D> simV = new List<Vector3D>();
        public int time = 0;
        /*飛行資訊*/
        public List<DataTable> dt = new List<DataTable>();
        public List<DataGridView> dataGridViews = new List<DataGridView>();
        public List<Label> labels = new List<Label>();
        public List<UdpClient> udpClients = new List<UdpClient>();
        public List<byte[]> UdpDatas = new List<byte[]>();
        public List<int> ports = new List<int>();
        public List<Buffer> buffers = new List<Buffer>();
        public DataTable FlightDistanceData = new DataTable();

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
            gMapControl1.Overlays.Add(Warning);
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

            Form1 myForm = new Form1();
            myForm.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int n = 0; n < udpClients.Count; n++)
            {
                double V = Math.Sqrt(buffers[n].vx * buffers[n].vx + buffers[n].vy * buffers[n].vy);
                V = double.Parse(V.ToString("F2"));

                Planes.AddPosition(buffers[n].lat, buffers[n].lng, planes[n].Id, buffers[n].heading, V, (int)gMapControl1.Zoom, planes[n]);

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
                        
                        /*
                        Bitmap pin = new Bitmap(21, 21);
                        for (int x = 0; x < pin.Width; x++)
                        {
                            for (int y = 0; y < pin.Height; y++)
                            {
                                pin.SetPixel(x, y, Color.FromArgb(R, G, B));
                            }
                        }*/
                        
                        GMapMarker marker = new GMarkerGoogle(point, GMarkerGoogleType.blue);
                        marker.ToolTipText = Convert.ToString( planes[n].Id + "\n" + (markers[n].Markers.Count + 1));
                        marker.ToolTip.Fill = Brushes.Transparent;
                        marker.ToolTip.Offset = new Point(-30, -15);
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
            else if(e.Button == MouseButtons.Middle)
            {
                planes.Add(new GMapOverlay(Name = textBox1.Text));
                gMapControl1.Overlays.Add(planes[planes.Count - 1]);
                PointLatLng latLng = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                Planes.AddPosition(latLng.Lat, latLng.Lng, textBox1.Text, 0, avgSpd, (int)gMapControl1.Zoom, planes[planes.Count - 1]);
                markers.Add(new GMapOverlay(Name = "markers" + planes.Count));
                gMapControl1.Overlays.Add(markers[markers.Count - 1]);
                dt.Add(new DataTable());
                routes.Add(new GMapRoute(Name = "routes" + planes.Count));
                points.Add(new List<PointLatLng>());
                simLLH.Add(new PointLatLng());
                simLLH[simLLH.Count - 1] = latLng;
                simXYZ.Add(new Point3D());
                simV.Add(new Vector3D());
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
            //無人機顯示
            for (int n = 0; n < planes.Count; n++)
            {
                if (markers[n].Markers.Count > 0)
                {
                    Point3D wpXYZ = new Point3D();
                    wpXYZ = CoordinateTransform.llh2xyz(markers[n].Markers[wp[n]].Position.Lat, markers[n].Markers[wp[n]].Position.Lng, 30);
                    simXYZ[n] = CoordinateTransform.llh2xyz(simLLH[n].Lat, simLLH[n].Lng, 30);
                    double wpAngle = Math.Atan((markers[n].Markers[wp[n]].Position.Lng - simLLH[n].Lng)
                                              /(markers[n].Markers[wp[n]].Position.Lat - simLLH[n].Lat)) * 180 / Math.PI;
                    double plane2wp = Point3D.Subtract(wpXYZ, simXYZ[n]).Length;

                    simV[n] = Point3D.Subtract(wpXYZ, simXYZ[n]) * avgSpd / plane2wp;
                    
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
                    
                    if (plane2wp < 10)
                    {
                        isReached[n] = true;

                        if (wp[n] < markers[n].Markers.Count - 1)
                        {
                            wp[n] += 1;
                            isReached[n] = false;
                        }
                        else
                            simV[n] *= 0;
                    }

                    //測試PF避障
                    if(isReached[n] == false)
                    {
                        for (int m = n; m < planes.Count; m++) //separation distance & time to collision surveillance
                        {
                            if (n != m)
                            {
                                int i = n * planes.Count - (n + 1) * n / 2 + (m - n) - 1; //第n台無人機與第m台無人機之代碼
                                isWarning[i] = CollisionAvoidance.detectTTC(simXYZ[n], simXYZ[m], simV[n], simV[m]);
                                distance2UAV[i] = Point3D.Subtract(simXYZ[n], simXYZ[m]).Length;
                                
                                if (isWarning[i] == true || distance2UAV[i]<=400)
                                {
                                    simV[n] = CollisionAvoidance.PF(planes.Count, simXYZ[n], simXYZ, wpXYZ);
                                    double V = simV[n].Length;
                                    simV[n] *= avgSpd / V;
                                }
                            }
                        }
                        /*
                        simV[n] = CollisionAvoidance.PF(planes.Count, simXYZ[n], simXYZ, wpXYZ);

                        double V = simV[n].Length;
                        simV[n] *= avgSpd / V;*/
                    }
                    else
                        button6.PerformClick();
                    
                    simXYZ[n] += simV[n];
                    
                    double[] LLH = CoordinateTransform.xyz2llh(simXYZ[n].X, simXYZ[n].Y, simXYZ[n].Z);
                    PointLatLng latLng = new PointLatLng();
                    latLng.Lat = LLH[0];
                    latLng.Lng = LLH[1];
                    simLLH[n] = latLng;

                    for (int i = 0; i < planes[n].Markers.Count; i++)
                        planes[n].Markers[i].IsVisible = false;
                    Planes.AddPosition(simLLH[n].Lat, simLLH[n].Lng, planes[n].Id, yaw[n], avgSpd, (int)gMapControl1.Zoom, planes[n]);
                    //TCAS.TAbubble(simLLH[n].Lat, simLLH[n].Lng, avgSpd, (int)gMapControl1.Zoom, planes[n]);

                    //飛行軌跡
                    if (planes[n].Markers.Count > 1)
                    {
                        Image srcRoute = Image.FromFile("dot_red.png");
                        Bitmap picRoute = (Bitmap)srcRoute;

                        for (int i = 0; i < planes[n].Markers.Count - 1; i++)
                        {
                            GMapMarker route = new GMarkerGoogle(new PointLatLng(planes[n].Markers[i].Position.Lat, planes[n].Markers[i].Position.Lng), picRoute);
                            route.Offset = new Point(-picRoute.Width / 2, -picRoute.Height / 2);
                            planes[n].Markers.RemoveAt(i);
                            planes[n].Markers.Insert(i, route);
                        }
                        
                        if (planes[n].Markers.Count > 51)
                        {
                            for (int i = 0; i < planes[n].Markers.Count - 51; i++)
                                planes[n].Markers[i].IsVisible = false;
                        }
                    }

                    if(gMapControl1.Zoom < 14)
                    {
                        for (int i = 0; i < planes[n].Markers.Count - 1; i++)
                            planes[n].Markers[i].IsVisible = false;
                    }
                        
                    labels[n].Text = planes[n].Id + ": distance to waypoint " + (wp[n]+1) + " is " + (int)plane2wp + " m";
                }
                if (dataGridView1.Rows.Count <= planes.Count)
                    dataGridView1.Rows.Add(new Object[] { planes[n].Id, simLLH[n].Lat, simLLH[n].Lng, 30, (int)yaw[n], avgSpd });
                else
                {
                    dataGridView1.Rows.RemoveAt(n);
                    dataGridView1.Rows.Insert(n, new Object[] { planes[n].Id, simLLH[n].Lat, simLLH[n].Lng, 30, (int)yaw[n], avgSpd });
                }

                //Planes.NewWaypoint(isReached[n], random.Next(-90,90), random.Next(-180,180), markers[n], planes[n], points[n], dt[n]);
                Planes.NewWaypoint(isReached[n], 22.0 + (random.Next(931315, 1024875) / 1000000.0), 120.0 + (random.Next(168747, 264191) / 1000000.0), markers[n], planes[n], points[n], dt[n]);
                
                if(time >= timer2.Interval * 5)
                {
                    DataRow dataRow = FlightDistanceData.NewRow();
                    for (int i = 0; i < distance2UAV.Count; i++)
                        dataRow[i] = distance2UAV[i];
                    FlightDistanceData.Rows.Add(dataRow);
                }
            }

            //碰撞時間顯示
            for (int i = 0; i < isWarning.Count; i++)
            {
                int k = i + 1;
                int a = 1;
                for (int n = 0; n < planes.Count; n++)
                {
                    if (k > planes.Count - (n + 1))
                    {
                        k -= planes.Count - (n + 1);
                        a += 1;
                    }
                    else
                        break;
                }
                int b = k + a;

                if (isWarning[i] == true)
                {
                    double approachRate = Vector3D.Subtract(simV[a - 1], simV[b - 1]).Length;
                    double time = Point3D.Subtract(simXYZ[a - 1], simXYZ[b - 1]).Length / approachRate;
                    Point3D CPAo = simXYZ[a - 1] + simV[a - 1] * time;
                    Point3D CPAi = simXYZ[b - 1] + simV[b - 1] * time;
                    Point3D CPA = new Point3D((CPAo.X + CPAi.X) / 2, (CPAo.Y + CPAi.Y) / 2, (CPAo.Z + CPAi.Z) / 2);
                    double[] LLH = CoordinateTransform.xyz2llh(CPA.X, CPA.Y, CPA.Z);
                    PointLatLng CPAllh = new PointLatLng(LLH[0], LLH[1]);

                    Warning.Markers[i].IsVisible = true;
                    Warning.Markers[i].Position = CPAllh;
                    Warning.Markers[i].ToolTipText = planes[a - 1].Id + " and " + planes[b - 1].Id + " TTC: " + (int)time + " sec";  //第a台飛機與第b台飛機的碰撞預警顯示
                    Warning.Markers[i].ToolTipMode = MarkerTooltipMode.Always;
                }
                else
                    Warning.Markers[i].IsVisible = false;

                if (distance2UAV[i] <= 400)
                    Console.WriteLine(planes[a - 1].Id + " and " + planes[b - 1].Id + " distance is too close." + " distance:" + (int)distance2UAV[i]);
            }

            time += timer2.Interval;
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Start Mission")
            {
                if (checkBox1.Checked)
                {
                    timer2.Enabled = true;
                    timer2.Start();
                    timer2.Interval = 100;
                    button3.Text = "Stop Mission";

                    if(isWarning.Count == 0)
                    {
                        for (int i = 0; i < planes.Count * (planes.Count - 1) / 2; i++)
                        {
                            isWarning.Add(false);
                            GMapMarker warn = new GMarkerGoogle(new PointLatLng(0, 0), GMarkerGoogleType.black_small);
                            warn.IsVisible = false;
                            Warning.Markers.Add(warn);
                        }

                        for (int n = 0; n < planes.Count; n++)
                        {
                            for (int m = n+1; m < planes.Count; m++)
                                FlightDistanceData.Columns.Add(planes[n].Id + "&" + planes[m].Id, typeof(double));
                        }
                    }

                    if (distance2UAV.Count == 0)
                    {
                        for (int i = 0; i < planes.Count * (planes.Count - 1) / 2; i++)
                            distance2UAV.Add(new double());
                    }
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
                    
                    record.CreateCSVFile(FlightDistanceData, "Data.csv");  //寫入CSV檔
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
                    double randomLat = 22.0 + (random.Next(931315, 1024875) / 1000000.0); //range:10km^2
                    double randomLng = 120.0 + (random.Next(168747, 264191) / 1000000.0);
                    Planes.AddPosition(randomLat, randomLng, textBox1.Text, 0, avgSpd, (int)gMapControl1.Zoom, planes[planes.Count - 1]);
                    markers.Add(new GMapOverlay(Name = "markers" + planes.Count));
                    gMapControl1.Overlays.Add(markers[markers.Count - 1]);
                    dt.Add(new DataTable());
                    routes.Add(new GMapRoute(Name = "routes" + planes.Count));
                    points.Add(new List<PointLatLng>());
                    simLLH.Add(new PointLatLng());
                    simLLH[simLLH.Count - 1] = new PointLatLng(randomLat, randomLng);
                    simXYZ.Add(new Point3D());
                    simV.Add(new Vector3D());
                    yaw.Add(0.0);
                    wp.Add(0);
                    isReached.Add(false);
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
                Thread.Sleep(10);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                textBox2.ReadOnly = true;
            else
                textBox2.ReadOnly = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            for (int n = 0; n < markers.Count; n++)
            {
                if(markers[n].Markers.Count == 0)
                {
                    double randomLat = 22.0 + (random.Next(931315, 1024875) / 1000000.0); //range:10km^2
                    double randomLng = 120.0 + (random.Next(168747, 264191) / 1000000.0);
                    PointLatLng latlng = new PointLatLng(randomLat, randomLng);
                    GMapMarker waypoint = new GMarkerGoogle(latlng, GMarkerGoogleType.blue);
                    waypoint.ToolTipText = Convert.ToString(planes[n].Id + "\n" + (markers[n].Markers.Count + 1));
                    waypoint.ToolTip.Fill = Brushes.Transparent;
                    waypoint.ToolTip.Offset = new Point(-30, -15);
                    waypoint.ToolTip.Stroke.Color = Color.Transparent;
                    waypoint.ToolTip.Foreground = Brushes.Black;
                    waypoint.ToolTip.Font = new Font("Arial", 10);
                    waypoint.ToolTipMode = MarkerTooltipMode.Always;
                    waypoint.IsVisible = waypoint.IsMouseOver;
                    markers[n].Markers.Add(waypoint);
                    points[n].Add(latlng);

                    dt[n].Rows.Add(new Object[] { markers[n].Markers.Count, (int)(latlng.Lat * 10000000), (int)(latlng.Lng * 10000000), 30, 0 });
                }
            }

            comboBox1.Enabled = false;
        }

        private void gMapControl1_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            for (int n = 0; n < planes.Count; n++)
            {
                if (item.Overlay == planes[n])
                    gMapControl1.Position = planes[n].Markers[planes[n].Markers.Count - 1].Position;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                Warning.IsVisibile = true;
            else
                Warning.IsVisibile = false;
        }
    }
}
