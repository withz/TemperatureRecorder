using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;

namespace Temperature
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private int bVoltage = 0;
        private int cTempe = 0;
        private int maxTempVal, minTempVal;
        private double figureMargin = 20;
        int displayCount = 500;
        int displayScale = 10;
        int displayStart = 1;
        double dataPosVal;
        double dataPosMaxVal;
        private bool lockRecentData = true;
        TextBlock[] xlabels;
        private int countPerSecond = 1000;

        static Polyline pl = new Polyline();

        public MainWindow()
        {
            InitializeComponent();
            int.TryParse(maxTemp.Text, out maxTempVal);
            int.TryParse(minTemp.Text, out minTempVal);
            xlabels = new TextBlock[6];
            
            pl.Stroke = Brushes.Red;
            pl.StrokeThickness = 1.5;
            dataMap.Children.Add(pl);
            for (int i = 0; i < 6; i++)
            {
                xlabels[i] = new TextBlock();
                xlabels[i].Foreground = Brushes.Black;
                xlabels[i].Width = 80;
                xlabels[i].TextAlignment = TextAlignment.Center;
                dataMap.Children.Add(xlabels[i]);
            }
            

            Worker draw = new Worker("draw", new Worker.WorkContent(() =>
            {
                while (true)
                {
                    int operationTimes = 0;
                    Thread.Sleep(100);

                    try
                    {
                        while (Record.recordQueue.Count != 0 && operationTimes < 3000)
                        {
                            Record.recordList.Add(Record.recordQueue.Dequeue());
                            operationTimes++;
                        }
                    }
                    catch(Exception ex)
                    {
                        //MessageBox.Show("ex");
                    }
                    
                    List<int[]> r;
                    try
                    {
                        if (Record.recordList.Count < 1)
                        {
                            continue;
                        }
                        
                        r = new List<int[]>();
                        if (lockRecentData == true)
                        {
                            List<int[]> rtemp = new List<int[]>();
                            //rtemp = Record.recordList.Reverse<int[]>().Take<int[]>(displayCount * displayScale).Reverse<int[]>().ToList();
                            int begin = Record.recordList.Count - displayScale * displayCount;
                            int end = Record.recordList.Count;
                            if (begin < 0)
                            {
                                begin = 0;
                            }
                            for (int i = begin; i < end; i++)
                            {
                                rtemp.Add(Record.recordList.ElementAt<int[]>(i));
                            }


                            int item = 0;
                            foreach (int[] x in rtemp)
                            {
                                if (item % displayScale == 0)
                                {
                                    r.Add(x);
                                }
                                item++;
                            }
                        }
                        else
                        {
                            int ps = (int)(dataPosVal / dataPosMaxVal * (Record.recordList.Count));
                            if (ps< displayCount * displayScale)
                            {
                                ps = displayCount * displayScale;
                            }
                            List<int[]> rtemp = new List<int[]>();
                            //rtemp = Record.recordList.Take<int[]>(ps).ToList();
                            //rtemp = rtemp.Reverse<int[]>().Take<int[]>(displayCount * displayScale).Reverse<int[]>().ToList();
                            int begin = ps - displayScale * displayCount;
                            int end = ps;
                            if (begin < 0)
                            {
                                begin = 0;
                            }
                            for (int i = begin; i < end; i++)
                            {
                                rtemp.Add(Record.recordList.ElementAt<int[]>(i));
                            }
                            int item = 0;
                            foreach (int[] x in rtemp)
                            {
                                if (item % displayScale == 0)
                                {
                                    r.Add(x);
                                }
                                item++;
                            }
                        }
                        if (r.Count < 1)
                        {
                            continue;
                        }
                        
                        double width, height;
                        width = figure.ActualWidth;
                        height = figure.ActualHeight;
                        if(width < 1 || height < 1)
                        {
                            continue;
                        }


                        displayStart = r[0][0];

                        double xdert, ydert;
                        xdert = (width - 2 * figureMargin) / (displayCount * displayScale);
                        ydert = (height - 2 * figureMargin) / (maxTempVal - minTempVal);

                        this.Dispatcher.BeginInvoke(new ModifyUI(() =>
                        {
                            int i;
                            for (i = 0; i <= 5; i++)
                            {
                                xlabels[i].Text = ((r[0][0] + displayCount * displayScale / 5 * i - 1)/ countPerSecond).ToString(".0");
                            }
                            countDataPacket.Content = Record.recordList.Count.ToString();
                            if (lockRecentData)
                            {
                                dataMapPos.Value = dataMapPos.Maximum;
                            }

                            dataMapPos.Maximum = Record.recordList.Count;
                            dataPosMaxVal = dataMapPos.Maximum;

                            if (Record.recordList.Last<int[]>() != null)
                            {
                                hotTemp.Content = Record.recordList.Last<int[]>()[1];
                            }
                            coldTemp.Content = cTempe;
                            batVoltage.Content = bVoltage;

                            //temp
                            if (pl == null)
                            {
                                pl = new Polyline();
                                pl.Stroke = Brushes.Red;
                                pl.StrokeThickness = 1.5;
                                dataMap.Children.Add(pl);
                            }

                            if (pl != null)
                            {
                                pl.Points.Clear();
                                foreach (int[] d in r)
                                {
                                    if(d != null)
                                    {
                                        pl.Points.Add(new Point(
                                       (d[0] - displayStart) * xdert + figureMargin,
                                       height - figureMargin - (d[1] - minTempVal) * (height - figureMargin * 2) / (maxTempVal - minTempVal)
                                       ));
                                    }
                                }
                            }
                            
                            r.Clear();
                        }));
                    }
                    catch (Exception exp)
                    {
                        //MessageBox.Show(exp.ToString());
                        continue;
                    }
                }

            }));
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private bool serverState = false;
        private Socket server;
        private Socket client;
        private delegate void ModifyUI();
        
        private void openServer_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (serverState == false)
            {
                btn.Content = "关闭端口";
                serverState = true;

                int port;
                int.TryParse(portText.Text, out port);
                server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                EndPoint ep = new IPEndPoint(new IPAddress(0), port);
                server.Bind(ep);
                server.Listen(0);
                stateLabel.Text = "端口已打开";

                Worker wk = new Worker("open_server", new Worker.WorkContent(() =>
                {
                    while (true)
                    {
                        try
                        {
                            client = server.Accept();
                            this.Dispatcher.BeginInvoke(new ModifyUI(() => {
                                countClient.Content = 1;
                            }));
                            
                        }
                        catch (Exception ex)
                        {
                            this.Dispatcher.BeginInvoke(new ModifyUI(() => {
                                countClient.Content = 0;
                            }));
                            server.Dispose();
                            //MessageBox.Show(ex.ToString());
                            break;
                        }

                        while (true)
                        {
                            try
                            {
                                byte[] buffer = new byte[10240];
                                int num = client.Receive(buffer);

                                if (num == 0)
                                {
                                    throw new Exception("disconnect");
                                }
                                byte[] split = new byte[3] { 0xfe, 0x01, 0xfe };
                                try
                                {
                                    for (int i = 0; i < num - 5; i++)
                                    {
                                        if (buffer[i] == split[0] && buffer[i + 1] == split[1] && buffer[i + 2] == split[2])
                                        {
                                            byte[] b = new byte[3];
                                            for (int j = 0; j < 3; j++)
                                            {
                                                b[j] = buffer[i + j + 3];
                                            }
                                            int temp = b[1] * 256 + b[2];
                                            switch (b[0])
                                            {
                                                case 0X55:
                                                    {
                                                        Record.NewRecord(temp);
                                                    }
                                                    break;
                                                case 0X33:
                                                    {
                                                        cTempe = temp/256;
                                                    }
                                                    break;
                                                case 0X35:
                                                    {
                                                        bVoltage = temp/256;
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception epc)
                                {
                                    //MessageBox.Show(epc.ToString());
                                }
                            }
                            catch (Exception exp)
                            {
                                this.Dispatcher.BeginInvoke(new ModifyUI(() => {
                                    countClient.Content = 0;
                                }));
                                client.Dispose();
                                //MessageBox.Show(exp.ToString());
                                break;
                            }
                        }
                        
                    }
                }));
            }
            else
            {
                btn.Content = "打开端口";
                serverState = false;
                stateLabel.Text = "端口已关闭";
                if (server != null)
                {
                    server.Dispose();
                }
                if (client != null)
                {
                    client.Dispose();
                }
            }
            
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RePaintFigure();
        }

        private void RePaintFigure()
        {
            figure.Children.Clear();
            double w, h;
            w = figure.ActualWidth;
            h = figure.ActualHeight;
            double margin = figureMargin;

            double thicks = 2;
            Line x1 = FigureLine(margin, h - margin, w - margin, h - margin, Brushes.Black, thicks);
            Line y1 = FigureLine(margin, margin, margin, h - margin, Brushes.Black, thicks);
            Line x2 = FigureLine(margin, margin, w - margin, margin, Brushes.Black, thicks);
            Line y2 = FigureLine(w - margin, margin, w - margin, h - margin, Brushes.Black, thicks);
            figure.Children.Add(x1);
            figure.Children.Add(x2);
            figure.Children.Add(y1);
            figure.Children.Add(y2);

            int i;
            TextBlock[] ylabels = new TextBlock[11];
            for (i = 0; i <= 10; i++)
            {
                ylabels[i] = new TextBlock();
                ylabels[i].Foreground = Brushes.Black;
                ylabels[i].Height = 16;
                figure.Children.Add(ylabels[i]);

                ylabels[i].Text = (maxTempVal - (maxTempVal - minTempVal) / 10 * i).ToString();
                Canvas.SetLeft(ylabels[i], 0);
                Canvas.SetTop(ylabels[i], ((h - 2 * margin) / 10 * i) + margin - ylabels[i].Height / 2);
            }

            double space;
            space = (w - figureMargin * 2) / 5;
            for (i = 0; i < 6; i++)
            {
                Canvas.SetLeft(xlabels[i], figureMargin + space * i - xlabels[i].Width / 2);
                Canvas.SetTop(xlabels[i], h - figureMargin + 1);
            }
        }

        private void RePaintMousePos(double x, double y)
        {
            double w, h;
            w = mousePos.ActualWidth;
            h = mousePos.ActualHeight;
            mousePos.Children.Clear();
            if (x < 0 && y < 0)
            {
                return;
            }
            if (x > w && y < 0)
            {
                return;
            }
            if (x > w && y > h)
            {
                return;
            }
            if (x < 0 && y > h)
            {
                return;
            }
            Line xl = FigureLine(0, y, w, y, Brushes.Gray, 0.5);
            Line yl = FigureLine(x, 0, x, h, Brushes.Gray, 0.5);
            mousePos.Children.Add(xl);
            mousePos.Children.Add(yl);
        }

        private Line FigureLine(double x1,double y1,double x2,double y2,SolidColorBrush cr,double tk)
        {
            Line l = new Line();
            l.Stroke = cr;
            l.StrokeThickness = tk;
            l.X1 = x1;
            l.Y1 = y1;
            l.X2 = x2;
            l.Y2 = y2;
            return l;
        }

        private void setRange_Click(object sender, RoutedEventArgs e)
        {
            int max, min, sca;
            int.TryParse(maxTemp.Text, out max);
            int.TryParse(minTemp.Text, out min);
            int.TryParse(dataScale.Text, out sca);

            if (max < min)
            {
                MessageBox.Show("上限不能低于下限");
                maxTemp.Text = maxTempVal.ToString();
                minTemp.Text = minTempVal.ToString();
                dataScale.Text = displayScale.ToString();
                return;
            }
            if (sca < 1)
            {
                MessageBox.Show("放大值无效");
                maxTemp.Text = maxTempVal.ToString();
                minTemp.Text = minTempVal.ToString();
                dataScale.Text = displayScale.ToString();
                return;
            }
            maxTempVal = max;
            minTempVal = min;
            displayScale = sca;
            RePaintFigure();
        }

        private void mousePos_MouseMove(object sender, MouseEventArgs e)
        {
            Canvas c = sender as Canvas;
            double w = c.ActualWidth;
            double h = c.ActualHeight;
            double px = e.GetPosition(c).X;
            double py = e.GetPosition(c).Y;

            RePaintMousePos(px, py);
            py = (maxTempVal - minTempVal) * (h - py - figureMargin) / (h - 2 * figureMargin) + minTempVal;
            px = ((displayCount*displayScale) * (px - figureMargin) / (w - 2 * figureMargin) + (displayStart - 1))/ countPerSecond;
            mousePosLabel.Content = px.ToString(".0") + "," + py.ToString(".");
        }

        private void saveTxt_Click(object sender, RoutedEventArgs e)
        {
            if (serverState == true)
            {
                MessageBox.Show("请先关闭端口再保存");
                return;
            }
            string filename;
            DateTime time = DateTime.Now;
            filename = time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second + "-" + "data.txt";
            string origin = stateLabel.Text;
            stateLabel.Text = "正在保存...";
            Button bt = sender as Button;
            bt.IsEnabled = false;
            Worker svwk = new Worker("savetxt", new Worker.WorkContent(() => 
            {
                try
                {
                    FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                    StreamWriter sw = new StreamWriter(fs);
                    foreach (int[] r in Record.recordList)
                    {
                        if (r != null)
                        {
                            sw.Write(r[0] + "\t" + r[1] + "\r\n");
                        }
                       
                    }
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.ToString());
                }
                
            }));
            stateLabel.Text = origin;
            bt.IsEnabled = true;
        }

        private void saveExcel_Click(object sender, RoutedEventArgs e)
        {
            if (serverState == true)
            {
                MessageBox.Show("请先关闭端口再保存");
                return;
            }
            string filename;
            DateTime time = DateTime.Now;
            filename = time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second + "-" + "data.csv";
            string origin = stateLabel.Text;
            stateLabel.Text = "正在保存...";
            Button bt = sender as Button;
            bt.IsEnabled = false;
            Worker svwk = new Worker("savecsv", new Worker.WorkContent(() =>
              {
                  try
                  {
                      FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                      StreamWriter sw = new StreamWriter(fs);
                      foreach (int[] r in Record.recordList)
                      {
                          if (r != null)
                          {
                              sw.Write(r[0] + "," + r[1] + "\n");
                          }
                      }
                      sw.Flush();
                      sw.Close();
                      fs.Close();
                  }
                  catch (Exception exp)
                  {
                      MessageBox.Show(exp.ToString());
                  }
                  
              }));
            stateLabel.Text = origin;
            bt.IsEnabled = true;
        }

        private void savePartTxt_Click(object sender, RoutedEventArgs e)
        {
            int start, end;
            int.TryParse(startSaveNum.Text, out start);
            int.TryParse(endSaveNum.Text, out end);
            if (start >= end || start < 0 || end < 0)
            {
                MessageBox.Show("保存范围有误");
                return;
            }
            if (serverState == true)
            {
                MessageBox.Show("请先关闭端口再保存");
                return;
            }
            int count = Record.recordList.Count;
            if (end > count)
            {
                MessageBox.Show("保存上限超出范围");
                return;
            }
            string filename;
            DateTime time = DateTime.Now;
            filename = time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second + "-" + "data.txt";
            string origin = stateLabel.Text;
            stateLabel.Text = "正在保存...";
            Button bt = sender as Button;
            bt.IsEnabled = false;
            Worker svwk = new Worker("savetxt", new Worker.WorkContent(() =>
            {
                try
                {
                    FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                    StreamWriter sw = new StreamWriter(fs);
                    for (int i = start; i < end; i++)
                    {
                        int[] r = Record.recordList[i];
                        if (r != null)
                        {
                            sw.Write(r[0] + "\t" + r[1] + "\r\n");
                        }
                    }
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.ToString());
                }
                
                this.Dispatcher.BeginInvoke(new ModifyUI(() =>
                {
                    stateLabel.Text = "端口已关闭";
                    bt.IsEnabled = true;
                }));
            }));
            
        }

        private void savePartExcel_Click(object sender, RoutedEventArgs e)
        {
            int start, end;
            int.TryParse(startSaveNum.Text, out start);
            int.TryParse(endSaveNum.Text, out end);
            if (start >= end || start < 0 || end < 0)
            {
                MessageBox.Show("保存范围错误");
                return;
            }
            if (serverState == true)
            {
                MessageBox.Show("请先关闭端口再保存");
                return;
            }
            int count = Record.recordList.Count;
            if (end >= count)
            {
                MessageBox.Show("保存上限超出范围");
                return;
            }
            string filename;
            DateTime time = DateTime.Now;
            filename = time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second + "-" + "data.csv";
            string origin = stateLabel.Text;
            stateLabel.Text = "正在保存...";
            Button bt = sender as Button;
            bt.IsEnabled = false;
            Worker svwk = new Worker("savecsv", new Worker.WorkContent(() =>
            {
                try
                {
                    FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
                    StreamWriter sw = new StreamWriter(fs);
                    for (int i = start; i <= end; i++)
                    {
                        int[] r = Record.recordList[i];
                        if (r != null)
                        {
                            sw.Write(r[0] + "," + r[1] + "\n");
                        }
                    }
                    sw.Flush();
                    sw.Close();
                    fs.Close();
                }
                catch(Exception exp)
                {
                    MessageBox.Show(exp.ToString());
                }
                
                this.Dispatcher.BeginInvoke(new ModifyUI(() =>
                {
                    stateLabel.Text = "端口已关闭";
                    bt.IsEnabled = true;
                }));
            }));
            
        }

        private void lockData_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            if (lockRecentData == true)
            {
                bt.Content = "保持显示最新数据";
                lockRecentData = false;
            }else
            {
                bt.Content = "取消保持最新数据";
                lockRecentData = true;
            }
        }

        private void dataMapPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dataPosVal = dataMapPos.Value;
            dataPosMaxVal = dataMapPos.Maximum;
        }

        private void resetAll_Click(object sender, RoutedEventArgs e)
        {
            if (serverState == true)
            {
                MessageBox.Show("请先关闭端口再保存");
                return;
            }
            MessageBoxResult mr = MessageBox.Show("确定清空数据?","清空数据",MessageBoxButton.OKCancel);
            if (mr == MessageBoxResult.OK)
            {
                Record.recordList.Clear();
                Record.recordQueue.Clear();
                pl.Points.Clear();
                Record.num = 0;
                for(int i = 0; i <= 5; i++)
                {
                    xlabels[i].Text = "";
                }
                hotTemp.Content = 0;
                coldTemp.Content = 0;
                batVoltage.Content = 0;
                countDataPacket.Content = 0;
                displayStart = 1;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Worker.WorkerExit();
        }
    }
}
