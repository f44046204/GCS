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
        public DataTable dt1 = new DataTable("Waypoints1");
        public DataTable dt2 = new DataTable("Waypoints2");
        public DataTable dt3 = new DataTable("Waypoints3");

        public double[] nedP1 = new double[3];
        public double[] nedP2 = new double[3];
        public double[] difP1P2 = new double[3];
        public double v1, v2, v3;

        public Buffer buffer1;
        public Buffer buffer2;
        public Buffer buffer3;

        public GMapOverlay planes1 = new GMapOverlay("planes1");
        public GMapOverlay planes2 = new GMapOverlay("planes2");
        public GMapOverlay planes3 = new GMapOverlay("planes3");
        public GMapOverlay markers1 = new GMapOverlay("markers1");
        public GMapOverlay markers2 = new GMapOverlay("markers2");
        public GMapOverlay markers3 = new GMapOverlay("markers3");
        public GMapRoute route1 = new GMapRoute("route1");
        public GMapRoute route2 = new GMapRoute("route2");
        public GMapRoute route3 = new GMapRoute("route3");
        public List<PointLatLng> points1 = new List<PointLatLng>();
        public List<PointLatLng> points2 = new List<PointLatLng>();
        public List<PointLatLng> points3 = new List<PointLatLng>();

        /*UDP 串口*/
        public UdpClient udpClient1 = new UdpClient(14550);
        public UdpClient udpClient2 = new UdpClient(14450);
        public UdpClient udpClient3 = new UdpClient(14350);

        /*接收資料*/
        public byte[] UdpData1 = new byte[1024];
        public byte[] UdpData2 = new byte[1024];
        public byte[] UdpData3 = new byte[1024];

        /*模擬用參數*/
        public PointLatLng simP1llh = new PointLatLng();
        public PointLatLng simP2llh = new PointLatLng();
        public PointLatLng simP3llh = new PointLatLng();
        public double[] simP1ned = new double[3];
        public double[] simP2ned = new double[3];
        public double[] simP3ned = new double[3];
        public double[] simV1 = new double[3];
        public double[] simV2 = new double[3];
        public double[] simV3 = new double[3];
        public double yaw1 = 0;
        public double yaw2 = 0;
        public double yaw3 = 0;
        public int wp1 = 0;
        public int wp2 = 0;
        public int wp3 = 0;
        public double yawRate = 10.0;
        public double avgSpd = 1.0;
        
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
            gMapControl1.Overlays.Add(planes1);
            gMapControl1.Overlays.Add(planes2);
            gMapControl1.Overlays.Add(planes3);
            gMapControl1.Overlays.Add(markers1);
            gMapControl1.Overlays.Add(markers2);
            gMapControl1.Overlays.Add(markers3);
            
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

            udpReceiveUdpThread.Start();
            
            timer1.Enabled = true;
            timer1.Start();
            timer1.Interval = 100;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            timer1.Stop();
            dataGridView1.Rows.Clear();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (buffer1.lat != 0 && buffer1.lng != 0)
            {
                v1 = Math.Sqrt(Math.Pow(buffer1.vx, 2) + Math.Pow(buffer1.vy, 2));
                v1 = double.Parse(v1.ToString("F2"));

                Planes.AddPosition(buffer1.lat, buffer1.lng, "UAV1", buffer1.heading, v1, planes1);

                nedP1 = CoordinateTransform.llh2ned(buffer1.lat, buffer1.lng, buffer1.height);

                /*if (Communication.planes1.Markers.Count > 1)
                { 
                    Communication.planes1.Markers[i].IsVisible = false;
                    i++;
                }*/

                /*測試路徑*/
                if (planes1.Markers.Count > 1)
                {
                    Image srcRoute = Image.FromFile("dot_red.png");
                    Bitmap picRoute = (Bitmap)srcRoute;

                    for (int i = 0; i < planes1.Markers.Count - 1; i++)
                    {
                        GMapMarker route = new GMarkerGoogle(new PointLatLng(planes1.Markers[i].Position.Lat, planes1.Markers[i].Position.Lng), picRoute);
                        planes1.Markers.RemoveAt(i);
                        planes1.Markers.Insert(i, route);
                    }

                    if (planes1.Markers.Count > 51)
                    {
                        for (int i = 0; i < planes1.Markers.Count - 51; i++)
                            planes1.Markers[i].IsVisible = false;
                    }
                }
            }

            if (buffer2.lat != 0 && buffer2.lng != 0)
            {
                v2 = Math.Sqrt(Math.Pow(buffer2.vx, 2) + Math.Pow(buffer2.vy, 2));
                v2 = double.Parse(v2.ToString("F2"));

                Planes.AddPosition(buffer2.lat, buffer2.lng, "UAV2", buffer2.heading, v2, planes2);

                nedP2 = CoordinateTransform.llh2ned(buffer2.lat, buffer2.lng, buffer2.height);

                /*if (planes2.Markers.Count > 1)
                {
                    planes2.Markers[j].IsVisible = false;
                    j++;
                }*/

                if (planes2.Markers.Count > 1)
                {
                    Image srcRoute = Image.FromFile("dot_blue.png");
                    Bitmap picRoute = (Bitmap)srcRoute;

                    for (int i = 0; i < planes2.Markers.Count - 1; i++)
                    {
                        GMapMarker route = new GMarkerGoogle(new PointLatLng(planes2.Markers[i].Position.Lat, planes2.Markers[i].Position.Lng), picRoute);
                        planes2.Markers.RemoveAt(i);
                        planes2.Markers.Insert(i, route);
                    }

                    if (planes2.Markers.Count > 51)
                    {
                        for (int i = 0; i < planes2.Markers.Count - 51; i++)
                            planes2.Markers[i].IsVisible = false;
                    }
                }
            }

            if (buffer3.lat != 0 && buffer3.lng != 0)
            {
                v3 = Math.Sqrt(Math.Pow(buffer3.vx, 2) + Math.Pow(buffer3.vy, 2));
                v3 = double.Parse(v3.ToString("F2"));

                Planes.AddPosition(buffer3.lat, buffer3.lng, "UAV3", buffer3.heading, v3, planes3);

                /*if (planes3.Markers.Count > 1)
                {
                    planes3.Markers[k].IsVisible = false;
                    k++;
                }*/

                if (planes3.Markers.Count > 1)
                {
                    Image srcRoute = Image.FromFile("dot_green.png");
                    Bitmap picRoute = (Bitmap)srcRoute;

                    for (int i = 0; i < planes3.Markers.Count - 1; i++)
                    {
                        GMapMarker route = new GMarkerGoogle(new PointLatLng(planes3.Markers[i].Position.Lat, planes3.Markers[i].Position.Lng), picRoute);
                        planes3.Markers.RemoveAt(i);
                        planes3.Markers.Insert(i, route);
                    }

                    if (planes3.Markers.Count > 51)
                    {
                        for (int i = 0; i < planes3.Markers.Count - 51; i++)
                            planes3.Markers[i].IsVisible = false;
                    }
                }
            }

            double m1 = buffer1.vx / buffer1.vy;
            double m2 = buffer2.vx / buffer2.vy;
            double[] xyzInter = CoordinateTransform.CalculateIntersection(nedP1[1], nedP1[0], m1, nedP2[1], nedP2[0], m2);
            double[] llhInter = CoordinateTransform.xyz2llh(xyzInter[0], xyzInter[1], 2476279.31);
            Console.WriteLine(llhInter[0] + "," + llhInter[1] + "," + llhInter[2]);

            if (dataGridView1.Rows.Count > 2)
            {
                v1 = Math.Sqrt(Math.Pow(buffer1.vx, 2) + Math.Pow(buffer1.vy, 2));
                v2 = Math.Sqrt(Math.Pow(buffer2.vx, 2) + Math.Pow(buffer2.vy, 2));
                v3 = Math.Sqrt(Math.Pow(buffer3.vx, 2) + Math.Pow(buffer3.vy, 2));
                v1 = double.Parse(v1.ToString("F2"));
                v2 = double.Parse(v2.ToString("F2"));
                v3 = double.Parse(v3.ToString("F2"));

                dataGridView1.Rows.Clear();
                dataGridView1.Rows.Insert(0, "UAV1", buffer1.lat, buffer1.lng, buffer1.height, (int)buffer1.heading, v1);
                dataGridView1.Rows.Insert(1, "UAV2", buffer2.lat, buffer2.lng, buffer2.height, (int)buffer2.heading, v2);
                dataGridView1.Rows.Insert(2, "UAV3", buffer3.lat, buffer3.lng, buffer3.height, (int)buffer3.heading, v3);
            }
            else
            {
                v1 = Math.Sqrt(Math.Pow(buffer1.vx, 2) + Math.Pow(buffer1.vy, 2));
                v2 = Math.Sqrt(Math.Pow(buffer2.vx, 2) + Math.Pow(buffer2.vy, 2));
                v3 = Math.Sqrt(Math.Pow(buffer3.vx, 2) + Math.Pow(buffer3.vy, 2));
                v1 = double.Parse(v1.ToString("F2"));
                v2 = double.Parse(v2.ToString("F2"));
                v3 = double.Parse(v3.ToString("F2"));

                dataGridView1.Rows.Insert(0, "UAV1", buffer1.lat, buffer1.lng, buffer1.height, (int)buffer1.heading, v1);
                dataGridView1.Rows.Insert(1, "UAV2", buffer2.lat, buffer2.lng, buffer2.height, (int)buffer2.heading, v2);
                dataGridView1.Rows.Insert(2, "UAV3", buffer3.lat, buffer3.lng, buffer3.height, (int)buffer3.heading, v3);
            }
        }

        private void gMapControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button==MouseButtons.Right)
            {
                switch (comboBox1.Text)
                {
                    case "UAV1":
                        PointLatLng point = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                        GMapMarker marker = new GMarkerGoogle(point, GMarkerGoogleType.blue);
                        marker.ToolTipText = Convert.ToString(markers1.Markers.Count +1);
                        marker.ToolTip.Fill = Brushes.Transparent;
                        marker.ToolTip.Offset = new Point(marker.LocalPosition.X - 18, marker.LocalPosition.Y - 15);
                        marker.ToolTip.Stroke.Color = Color.Transparent;
                        marker.ToolTip.Foreground = Brushes.Black;
                        marker.ToolTip.Font = new Font("Arial", 10);
                        marker.ToolTipMode = MarkerTooltipMode.Always;
                        markers1.Markers.Add(marker);
                        points1.Add(point);
                        
                        if (markers1.Markers.Count > 1)
                        {
                            route1 = new GMapRoute(points1, "route1");
                            markers1.Routes.Add(route1);
                        }
                        
                        dt1.Rows.Add(new Object[] { (markers1.Markers.Count), (int)(point.Lat* 10000000), (int)(point.Lng* 10000000), 30, 0 });
                        
                        break;

                    case "UAV2":
                        PointLatLng point2 = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                        GMapMarker marker2 = new GMarkerGoogle(point2, GMarkerGoogleType.red);
                        marker2.ToolTipText = Convert.ToString(markers2.Markers.Count +1);
                        marker2.ToolTip.Fill = Brushes.Transparent;
                        marker2.ToolTip.Offset = new Point(marker2.LocalPosition.X - 18, marker2.LocalPosition.Y - 15);
                        marker2.ToolTip.Stroke.Color = Color.Transparent;
                        marker2.ToolTip.Foreground = Brushes.Black;
                        marker2.ToolTip.Font = new Font("Arial", 10);
                        marker2.ToolTipMode = MarkerTooltipMode.Always;
                        markers2.Markers.Add(marker2);
                        points2.Add(point2);

                        if (markers2.Markers.Count > 1)
                        {
                            route2 = new GMapRoute(points2, "route2");
                            markers2.Routes.Add(route2);
                        }

                        dt2.Rows.Add(new Object[] { (markers2.Markers.Count), (int)(point2.Lat * 10000000), (int)(point2.Lng * 10000000), 30, 0 });

                        break;

                    case "UAV3":
                        PointLatLng point3 = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                        GMapMarker marker3 = new GMarkerGoogle(point3, GMarkerGoogleType.green);
                        marker3.ToolTipText = Convert.ToString(markers3.Markers.Count +1);
                        marker3.ToolTip.Fill = Brushes.Transparent;
                        marker3.ToolTip.Offset = new Point(marker3.LocalPosition.X - 18, marker3.LocalPosition.Y - 15);
                        marker3.ToolTip.Stroke.Color = Color.Transparent;
                        marker3.ToolTip.Foreground = Brushes.Black;
                        marker3.ToolTip.Font = new Font("Arial", 10);
                        marker3.ToolTipMode = MarkerTooltipMode.Always;
                        markers3.Markers.Add(marker3);
                        points3.Add(point3);

                        if (markers3.Markers.Count > 1)
                        {
                            route3 = new GMapRoute(points3, "route3");
                            markers3.Routes.Add(route3);
                        }

                        dt3.Rows.Add(new Object[] { (markers3.Markers.Count), (int)(point3.Lat * 10000000), (int)(point3.Lng * 10000000), 30, 0 });

                        break;
                        
                    default:
                        MessageBox.Show("Invalid selection. Please select UAV.","Target needed.",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                        break;
                }
            }
            else if (e.Button==MouseButtons.Middle) //模擬用初始飛機
            {
                switch (comboBox1.Text)
                {
                    case "UAV1":
                        if (planes1.Markers.Count == 0)
                        {
                            
                            simP1llh = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                            Planes.AddPosition(simP1llh.Lat, simP1llh.Lng, "UAV1", 0, 0, planes1);
                        }
                        break;
                    case "UAV2":
                        if (planes2.Markers.Count == 0)
                        {
                            simP2llh = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                            Planes.AddPosition(simP2llh.Lat, simP2llh.Lng, "UAV2", 0, 0, planes2);
                        }
                        break;
                    case "UAV3":
                        if (planes3.Markers.Count == 0)
                        {
                            simP3llh = gMapControl1.FromLocalToLatLng(e.X, e.Y);
                            Planes.AddPosition(simP3llh.Lat, simP3llh.Lng, "UAV3", 0, 0, planes3);
                        }
                        break;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            switch (comboBox1.Text)
            {
                case "UAV1":
                    markers1.Clear();
                    points1.Clear();
                    dt1.Clear();
                    wp1 = 0;
                    break;
                case "UAV2":
                    markers2.Clear();
                    points2.Clear();
                    dt2.Clear();
                    wp2 = 0;
                    break;
                case "UAV3":
                    markers3.Clear();
                    points3.Clear();
                    dt3.Clear();
                    wp3 = 0;
                    break;
                default:
                    MessageBox.Show("Invalid selection. Please select UAV.", "Target needed.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        private void dataGridView2_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.Button==MouseButtons.Right)
            {
                dataGridView2.Rows[e.RowIndex].Selected = true;
                contextMenuStrip1.Show(ToolStripDropDown.MousePosition) ;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("確認刪除?", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                    int count;

                    foreach (DataRow row in dt1.Rows)
                    {
                        int seq;
                        if (Convert.ToInt32(row.ItemArray[0]) > Convert.ToInt32(dt1.Rows[dataGridView2.CurrentRow.Index].ItemArray[0]))
                        {
                            seq = Convert.ToInt32(row[0]) - 1;
                            row[0] = seq;
                            markers1.Markers[Convert.ToInt32(row.ItemArray[0])].ToolTipText = Convert.ToString(seq);
                        }
                    }

                if (markers1.Markers.Count > 1)
                {
                    dt1.Rows.RemoveAt(dataGridView2.CurrentRow.Index);
                    markers1.Markers.RemoveAt(dataGridView2.CurrentRow.Index);
                    markers1.Routes.Clear();
                    points1.RemoveAt(dataGridView2.CurrentRow.Index);
                    GMapRoute newRoute = new GMapRoute(points1, "route1");
                    markers1.Routes.Add(newRoute);

                    count = markers1.Markers.Count;
                    markers1.Markers[count - 1].ToolTipText = Convert.ToString(count);
                }
                else
                {
                    markers1.Markers.Clear();
                    points1.Clear();
                    dt1.Rows.Clear();
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView2.DataSource = dt1;
            dt1.Columns.Add("Sequence", typeof(int)); dataGridView2.Columns[0].Width = 70;
            dt1.Columns.Add("Lat", typeof(int)); dataGridView2.Columns[1].Visible = false;
            dt1.Columns.Add("Lng", typeof(int)); dataGridView2.Columns[2].Visible = false;
            dt1.Columns.Add("Alt", typeof(int)); dataGridView2.Columns[3].Width = 70;
            dt1.Columns.Add("Time", typeof(int)); dataGridView2.Columns[4].Width = 70;

            dataGridView3.DataSource = dt2;
            dt2.Columns.Add("Sequence", typeof(int)); dataGridView3.Columns[0].Width = 70;
            dt2.Columns.Add("Lat", typeof(int)); dataGridView3.Columns[1].Visible = false;
            dt2.Columns.Add("Lng", typeof(int)); dataGridView3.Columns[2].Visible = false;
            dt2.Columns.Add("Alt", typeof(int)); dataGridView3.Columns[3].Width = 70;
            dt2.Columns.Add("Time", typeof(int)); dataGridView3.Columns[4].Width = 70;

            dataGridView4.DataSource = dt3;
            dt3.Columns.Add("Sequence", typeof(int)); dataGridView4.Columns[0].Width = 70;
            dt3.Columns.Add("Lat", typeof(int)); dataGridView4.Columns[1].Visible = false;
            dt3.Columns.Add("Lng", typeof(int)); dataGridView4.Columns[2].Visible = false;
            dt3.Columns.Add("Alt", typeof(int)); dataGridView4.Columns[3].Width = 70;
            dt3.Columns.Add("Time", typeof(int)); dataGridView4.Columns[4].Width = 70;
        }

        private void dataGridView3_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                dataGridView3.Rows[e.RowIndex].Selected = true;
                contextMenuStrip2.Show(ToolStripDropDown.MousePosition);
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("確認刪除?", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                int count;

                foreach (DataRow row in dt2.Rows)
                {
                    int seq;
                    if (Convert.ToInt32(row.ItemArray[0]) > Convert.ToInt32(dt2.Rows[dataGridView3.CurrentRow.Index].ItemArray[0]))
                    {
                        seq = Convert.ToInt32(row[0]) - 1;
                        row[0] = seq;
                        markers2.Markers[Convert.ToInt32(row.ItemArray[0])].ToolTipText = Convert.ToString(seq);
                    }
                }

                if (markers2.Markers.Count > 1)
                {
                    dt2.Rows.RemoveAt(dataGridView3.CurrentRow.Index);
                    markers2.Markers.RemoveAt(dataGridView3.CurrentRow.Index);
                    markers2.Routes.Clear();
                    points2.RemoveAt(dataGridView3.CurrentRow.Index);
                    GMapRoute newRoute = new GMapRoute(points2, "route2");
                    markers2.Routes.Add(newRoute);

                    count = markers2.Markers.Count;
                    markers2.Markers[count - 1].ToolTipText = Convert.ToString(count);
                }
                else
                {
                    markers2.Markers.Clear();
                    points2.Clear();
                    dt2.Rows.Clear();
                }
            }
        }

        private void dataGridView4_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                dataGridView4.Rows[e.RowIndex].Selected = true;
                contextMenuStrip3.Show(ToolStripDropDown.MousePosition);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("確認刪除?", "提示", MessageBoxButtons.OKCancel);
            if (dr == DialogResult.OK)
            {
                int count;

                foreach (DataRow row in dt3.Rows)
                {
                    int seq;
                    if (Convert.ToInt32(row.ItemArray[0]) > Convert.ToInt32(dt3.Rows[dataGridView4.CurrentRow.Index].ItemArray[0]))
                    {
                        seq = Convert.ToInt32(row[0]) - 1;
                        row[0] = seq;
                        markers3.Markers[Convert.ToInt32(row.ItemArray[0])].ToolTipText = Convert.ToString(seq);
                    }
                }

                if (markers3.Markers.Count > 1)
                {
                    dt3.Rows.RemoveAt(dataGridView4.CurrentRow.Index);
                    markers3.Markers.RemoveAt(dataGridView4.CurrentRow.Index);
                    markers3.Routes.Clear();
                    points3.RemoveAt(dataGridView4.CurrentRow.Index);
                    GMapRoute newRoute = new GMapRoute(points3, "route3");
                    markers3.Routes.Add(newRoute);

                    count = markers3.Markers.Count;
                    markers3.Markers[count - 1].ToolTipText = Convert.ToString(count);
                }
                else
                {
                    markers3.Markers.Clear();
                    points3.Clear();
                    dt3.Rows.Clear();
                }
            }
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            dt1.Rows[r][c] = Convert.ToInt32(dataGridView2.CurrentCell.Value);
            //Console.WriteLine(dt1.Rows[r][c]);
        }

        private void dataGridView3_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            dt2.Rows[r][c] = Convert.ToInt32(dataGridView3.CurrentCell.Value);
            //Console.WriteLine(dt2.Rows[r][c]);
        }

        private void dataGridView4_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int r = e.RowIndex;
            int c = e.ColumnIndex;
            dt3.Rows[r][c] = Convert.ToInt32(dataGridView4.CurrentCell.Value);
            //Console.WriteLine(dt3.Rows[r][c]);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (markers1.Markers.Count > 0)
            {
                double wpAngle = Math.Atan((markers1.Markers[wp1].Position.Lng - planes1.Markers[planes1.Markers.Count - 1].Position.Lng) /
                                             (markers1.Markers[wp1].Position.Lat - planes1.Markers[planes1.Markers.Count - 1].Position.Lat)) * 180 / Math.PI;

                if (markers1.Markers[wp1].Position.Lat - planes1.Markers[planes1.Markers.Count - 1].Position.Lat < 0)
                    wpAngle += 180;

                //限制角度在0-360
                wpAngle = TCAS.angleLimit(wpAngle);
                yaw1 = TCAS.angleLimit(yaw1);

                double desireYaw = 0;
                desireYaw = wpAngle - yaw1;
                desireYaw = TCAS.angleLimit(desireYaw);

                //Console.WriteLine(wpAngle);
                
                if (desireYaw <= 180)   //轉向
                {
                    if (desireYaw > yawRate)
                        yaw1 += yawRate;
                    else
                        yaw1 = wpAngle;
                }
                else
                {
                    if (Math.Abs(360 - desireYaw) > yawRate)
                        yaw1 -= yawRate;
                    else
                        yaw1 = wpAngle;
                }

                if(yaw1 == wpAngle)     //速度
                {
                    simV1[0] = avgSpd * Math.Cos(yaw1 / 180 * Math.PI);
                    simV1[1] = avgSpd * Math.Sin(yaw1 / 180 * Math.PI);
                    simV1[2] = 0;
                }
                
                double[] WPned = CoordinateTransform.llh2ned(markers1.Markers[wp1].Position.Lat, markers1.Markers[wp1].Position.Lng, 30);
                double dis = Math.Sqrt(Math.Pow(WPned[0] - simP1ned[0], 2) + Math.Pow(WPned[1] - simP1ned[1], 2));
                //Console.WriteLine("distance:" + dis);
                if (dis < 5)
                {
                    if (wp1 < markers1.Markers.Count-1)
                        wp1 += 1;
                    else
                    {
                        for (int i = 0; i < 3; i++)
                            simV1[i] = 0;
                    }
                }
                label5.Text = "Heading to No. " + (wp1+1);
                
                simP1ned = CoordinateTransform.llh2ned(simP1llh.Lat, simP1llh.Lng, 30);
                simP1ned[0] += simV1[0];
                simP1ned[1] += simV1[1];
                simP1ned[2] += simV1[2];
                double[] XYZ = CoordinateTransform.ned2xyz(simP1ned[0], simP1ned[1], simP1ned[2]);
                double[] LLH = CoordinateTransform.xyz2llh(XYZ[0], XYZ[1], XYZ[2]);
                simP1llh.Lat = LLH[0];
                simP1llh.Lng = LLH[1];
               
                for (int i = 0; i < planes1.Markers.Count; i++)
                    planes1.Markers[i].IsVisible = false;
                Planes.AddPosition(simP1llh.Lat, simP1llh.Lng, "UAV1", yaw1, avgSpd*10, planes1);
                TCAS.TAbubble(simP1llh.Lat, simP1llh.Lng, avgSpd * 10, (int)gMapControl1.Zoom, planes1);
            }

            if (markers2.Markers.Count > 0)
            {
                double wpAngle = Math.Atan((markers2.Markers[wp2].Position.Lng - planes2.Markers[planes2.Markers.Count - 1].Position.Lng) /
                                             (markers2.Markers[wp2].Position.Lat - planes2.Markers[planes2.Markers.Count - 1].Position.Lat)) * 180 / Math.PI;

                if (markers2.Markers[wp2].Position.Lat - planes2.Markers[planes2.Markers.Count - 1].Position.Lat < 0)
                    wpAngle += 180;

                //限制角度在0-360
                wpAngle = TCAS.angleLimit(wpAngle);
                yaw2 = TCAS.angleLimit(yaw2);

                double desireYaw = 0;
                desireYaw = wpAngle - yaw2;
                desireYaw = TCAS.angleLimit(desireYaw);

                //Console.WriteLine(wpAngle);

                if (desireYaw <= 180)   //轉向
                {
                    if (desireYaw > yawRate)
                        yaw2 += yawRate;
                    else
                        yaw2 = wpAngle;
                }
                else
                {
                    if (Math.Abs(360 - desireYaw) > yawRate)
                        yaw2 -= yawRate;
                    else
                        yaw2 = wpAngle;
                }

                if (yaw2 == wpAngle)     //速度
                {
                    simV2[0] = avgSpd * Math.Cos(yaw2 / 180 * Math.PI);
                    simV2[1] = avgSpd * Math.Sin(yaw2 / 180 * Math.PI);
                    simV2[2] = 0;
                }

                double[] WPned = CoordinateTransform.llh2ned(markers2.Markers[wp2].Position.Lat, markers2.Markers[wp2].Position.Lng, 30);
                double dis = Math.Sqrt(Math.Pow(WPned[0] - simP2ned[0], 2) + Math.Pow(WPned[1] - simP2ned[1], 2));
                //Console.WriteLine("distance:" + dis);
                if (dis < 5)
                {
                    if (wp2 < markers2.Markers.Count - 1)
                        wp2 += 1;
                    else
                    {
                        for (int i = 0; i < 3; i++)
                            simV2[i] = 0;
                    }
                }
                //label5.Text = "Heading to No. " + (wp2 + 1);

                simP2ned = CoordinateTransform.llh2ned(simP2llh.Lat, simP2llh.Lng, 30);
                simP2ned[0] += simV2[0];
                simP2ned[1] += simV2[1];
                simP2ned[2] += simV2[2];
                double[] XYZ = CoordinateTransform.ned2xyz(simP2ned[0], simP2ned[1], simP2ned[2]);
                double[] LLH = CoordinateTransform.xyz2llh(XYZ[0], XYZ[1], XYZ[2]);
                simP2llh.Lat = LLH[0];
                simP2llh.Lng = LLH[1];

                for (int i = 0; i < planes2.Markers.Count; i++)
                    planes2.Markers[i].IsVisible = false;
                Planes.AddPosition(simP2llh.Lat, simP2llh.Lng, "UAV2", yaw2, avgSpd*10, planes2);
                TCAS.TAbubble(simP2llh.Lat, simP2llh.Lng, avgSpd * 10, (int)gMapControl1.Zoom, planes2);
            }

            if (markers3.Markers.Count > 0)
            {
                double wpAngle = Math.Atan((markers3.Markers[wp3].Position.Lng - planes3.Markers[planes3.Markers.Count - 1].Position.Lng) /
                                             (markers3.Markers[wp3].Position.Lat - planes3.Markers[planes3.Markers.Count - 1].Position.Lat)) * 180 / Math.PI;

                if (markers3.Markers[wp3].Position.Lat - planes3.Markers[planes3.Markers.Count - 1].Position.Lat < 0)
                    wpAngle += 180;

                //限制角度在0-360
                wpAngle = TCAS.angleLimit(wpAngle);
                yaw3 = TCAS.angleLimit(yaw3);

                double desireYaw = 0;
                desireYaw = wpAngle - yaw3;
                desireYaw = TCAS.angleLimit(desireYaw);

                //Console.WriteLine(wpAngle);

                if (desireYaw <= 180)   //轉向
                {
                    if (desireYaw > yawRate)
                        yaw3 += yawRate;
                    else
                        yaw3 = wpAngle;
                }
                else
                {
                    if (Math.Abs(360 - desireYaw) > yawRate)
                        yaw3 -= yawRate;
                    else
                        yaw3 = wpAngle;
                }

                if (yaw3 == wpAngle)     //速度
                {
                    simV3[0] = avgSpd * Math.Cos(yaw3 / 180 * Math.PI);
                    simV3[1] = avgSpd * Math.Sin(yaw3 / 180 * Math.PI);
                    simV3[2] = 0;
                }

                double[] WPned = CoordinateTransform.llh2ned(markers3.Markers[wp3].Position.Lat, markers3.Markers[wp3].Position.Lng, 30);
                double dis = Math.Sqrt(Math.Pow(WPned[0] - simP3ned[0], 2) + Math.Pow(WPned[1] - simP3ned[1], 2));
                Console.WriteLine("distance:" + dis);
                if (dis < 5)
                {
                    if (wp3 < markers3.Markers.Count - 1)
                        wp3 += 1;
                    else
                    {
                        for (int i = 0; i < 3; i++)
                            simV3[i] = 0;
                    }
                }
                //label5.Text = "Heading to No. " + (wp2 + 1);

                simP3ned = CoordinateTransform.llh2ned(simP3llh.Lat, simP3llh.Lng, 30);
                simP3ned[0] += simV3[0];
                simP3ned[1] += simV3[1];
                simP3ned[2] += simV3[2];
                double[] XYZ = CoordinateTransform.ned2xyz(simP3ned[0], simP3ned[1], simP3ned[2]);
                double[] LLH = CoordinateTransform.xyz2llh(XYZ[0], XYZ[1], XYZ[2]);
                simP3llh.Lat = LLH[0];
                simP3llh.Lng = LLH[1];

                for (int i = 0; i < planes3.Markers.Count; i++)
                    planes3.Markers[i].IsVisible = false;
                Planes.AddPosition(simP3llh.Lat, simP3llh.Lng, "UAV3", yaw3, avgSpd*10, planes3);
                TCAS.TAbubble(simP3llh.Lat, simP3llh.Lng, avgSpd * 10, (int)gMapControl1.Zoom, planes3);
            }

            if (dataGridView1.Rows.Count > 3)
            {
                dataGridView1.Rows.Clear();
                dataGridView1.Rows.Insert(0, "UAV1", planes1.Markers[planes1.Markers.Count-1].Position.Lat, planes1.Markers[planes1.Markers.Count - 1].Position.Lng, 30, (int)yaw1, avgSpd*10);
                dataGridView1.Rows.Insert(1, "UAV2", planes2.Markers[planes2.Markers.Count - 1].Position.Lat, planes2.Markers[planes2.Markers.Count - 1].Position.Lng, 30, (int)yaw2, avgSpd * 10);
                dataGridView1.Rows.Insert(2, "UAV3", planes3.Markers[planes3.Markers.Count - 1].Position.Lat, planes3.Markers[planes3.Markers.Count - 1].Position.Lng, 30, (int)yaw3, avgSpd * 10);
            }
            else
            {
                dataGridView1.Rows.Insert(0, "UAV1", planes1.Markers[planes1.Markers.Count - 1].Position.Lat, planes1.Markers[planes1.Markers.Count - 1].Position.Lng, 30, (int)yaw1, avgSpd*10);
                dataGridView1.Rows.Insert(1, "UAV2", planes2.Markers[planes2.Markers.Count - 1].Position.Lat, planes2.Markers[planes2.Markers.Count - 1].Position.Lng, 30, (int)yaw2, avgSpd * 10);
                dataGridView1.Rows.Insert(2, "UAV3", planes3.Markers[planes3.Markers.Count - 1].Position.Lat, planes3.Markers[planes3.Markers.Count - 1].Position.Lng, 30, (int)yaw3, avgSpd * 10);
            }
            /*
            bool Horizontal = TCAS.horizontalSelection(yaw1, planes1.Markers[planes1.Markers.Count - 1].Position, planes2.Markers[planes2.Markers.Count - 1].Position, simV1, simV2);
            if (Horizontal)
                label6.Text = "Horizontal Approach.";
            else
                label6.Text = "Surveillance.";*/
            double distance = Math.Sqrt(Math.Pow(simP3ned[0] - simP2ned[0], 2) + Math.Pow(simP3ned[1] - simP2ned[1], 2));
            Console.WriteLine(distance);
            label6.Text = gMapControl1.Zoom.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text == "Start Mission")
            {
                timer2.Enabled = true;
                timer2.Start();
                timer2.Interval = 100;
                button3.Text = "Stop Mission";
            }
            else
            {
                timer2.Enabled = false;
                timer2.Stop();
                dataGridView1.Rows.Clear();
                button3.Text = "Start Mission";
            }
            
        }
        
        public struct Buffer
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

                Thread.Sleep(10);
            }
        }
    }
}
