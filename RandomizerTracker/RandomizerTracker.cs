using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;
using System.Reflection;
using QuickGraph;
using GraphX;
using GraphX.Controls;
using System.Windows;
using static RandomizerTracker.RandomizerData;
using static RandomizerTracker.XmlData;

namespace RandomizerTracker
{
    public partial class RandomizerTracker : Form
    {
        public static RandomizerTracker instance;

        public RandomizerTracker()
        {
            instance = this;
            InitializeComponent();
            filepathBox.Text = LogFilepath;
            helperlogBox.Text = helperFilepath;
            ProcessXmls();
            GetRandomizerData();
            Load += (obj, e) => MasterRefresh();
            trackerLogViewer.VisibleChanged += ConstructManualInputPage;
        }

        #region Map setup and controls

        private ZoomControl zoom;
        private Grapher.GraphArea graph;

        private void buttonRefreshMap_Click(object sender, EventArgs e)
        {
            graph.Dispose();
            graph = Grapher.CreateTransitionGraph();
            zoom.Content = graph;
            graph.GenerateGraph(true);
            Grapher.RecolorVertices(graph);
        }

        private void MasterRefresh()
        {
            GetRandomizerData();

            if (graph != null) graph.Dispose();
            graph = Grapher.CreateTransitionGraph();

            if (zoom == null)
            {
                zoom = new ZoomControl();
                ZoomControl.SetViewFinderVisibility(zoom, Visibility.Visible);
                elementHost1.Child = zoom;
            }
            zoom.Content = graph;
            zoom.Zoom = 0.01f;

            zoomToVertexSelect.Items.Clear();
            zoomToVertexSelect.Items.AddRange(roomRandomizer ? rooms.ToArray() : areas.ToArray());

            roomInfoBox.Items.Clear();
            roomInfoBox.Items.AddRange(roomRandomizer ? rooms.ToArray() : areas.ToArray());
            roomStatsBox.Text = string.Empty;

            graph.GenerateGraph(true);
            Grapher.RecolorVertices(graph);
            edgeLabelsToggled = false;
            zoom.ZoomToFill();

            ToggleEdgeLabels.Text = "Show Edge Labels";
            toggleBenchesButton.Text = "Show Benches";
            helperButton.Text = "Show Helper Locations";

            Grapher.highlightMode = 0;

            string stattext = "";
            if (roomRandomizer)
            {
                stattext += string.Format("Explored {0} of {1} randomized transitions",
                    exploredTransitions.Count(), transitionToRoom.Count());
            }
            else if (areaRandomizer)
            {
                stattext += string.Format("Explored {0} of {1} randomized transitions",
                    exploredTransitions.Count(), transitionToArea.Count());
            }

            int exhaustedRooms = 0;
            foreach (var kvp in graph.VertexList)
            {
                if (!kvp.Key.ToString().Contains("\n"))
                {
                    exhaustedRooms += 1;
                }
            }

            if (roomRandomizer)
            {
                stattext += string.Format("\n\nVisited (and checked a transition in) {0} of {1} rooms",
                    exploredRooms.Count(), graph.VertexList.Count());


                stattext += string.Format(" - exhausted {0} rooms", exhaustedRooms);
            }
            else if (areaRandomizer)
            {
                stattext += string.Format("\n\nChecked a transition in {0} of {1} areas",
                    exploredRooms.Count(), graph.VertexList.Count());

                stattext += string.Format(" - exhausted {0} areas", exhaustedRooms);
            }
            
            stattext += string.Format("\n\nObtained {0} of {1} checks (shops excluded)",
                checkedLocations.Count(), randomizedItems.Where(item => !checkIfShopItem[item]).Count());

            StatisticsText.Text = stattext;
        }

        private void masterRefreshButton_Click(object sender, EventArgs e) => MasterRefresh();

        
        private void ToggleEdgeLabels_Click(object sender, EventArgs e)
        {
            edgeLabelsToggled = !edgeLabelsToggled;
            graph.ShowAllEdgesLabels(edgeLabelsToggled);
            if (edgeLabelsToggled)
            {
                ToggleEdgeLabels.Text = "Hide Edge Labels";
            }
            else
            {
                ToggleEdgeLabels.Text = "Show Edge Labels";
            }
        }

        private static bool edgeLabelsToggled = false;
        #endregion

        #region Manual input

        private void ConstructManualInputPage(object sender, EventArgs e)
        {
            if (!trackerLogViewer.Visible || !File.Exists(LogFilepath)) return;

            GetRandomizerData();
            RefreshTrackerLogViewer();

            itemSelectBox.Items.Clear();
            itemSelectBox.Items.AddRange(randomizedItems.ToArray());

            locationSelectBox.Items.Clear();
            locationSelectBox.Items.AddRange(randomizedItems.ToArray());

            sourceTransitionSelect.Items.Clear();
            targetTransitionSelect.Items.Clear();

            if (areaRandomizer)
            {
                sourceTransitionSelect.Items.AddRange(areaTransitions.ToArray());
                targetTransitionSelect.Items.AddRange(areaTransitions.ToArray());
            }
            else if (roomRandomizer)
            {
                sourceTransitionSelect.Items.AddRange(roomTransitions.ToArray());
                targetTransitionSelect.Items.AddRange(roomTransitions.ToArray());
            }
        }

        private void RefreshTrackerLogViewer()
        {
            trackerLogViewer.Items.Clear();
            if (!File.Exists(Properties.Settings.Default.filepath))
            {
                System.Windows.Forms.MessageBox.Show("Error: No tracker log found at the specified filepath.");
                return;
            }

            string[] data = File.ReadAllLines(Properties.Settings.Default.filepath);
            trackerLogViewer.Items.AddRange(data);
        }

        private void FastRefreshTrackerLogViewer(string[] data)
        {
            trackerLogViewer.Items.Clear();
            trackerLogViewer.Items.AddRange(data);
        }

        private void AddToTrackerLog(params string[] lines)
        {
            if (!File.Exists(Properties.Settings.Default.filepath))
            {
                System.Windows.Forms.MessageBox.Show("Error: No tracker log found at the specified filepath.");
                return;
            }

            File.AppendAllLines(LogFilepath, lines);
            RefreshTrackerLogViewer();
        }

        private void RemoveFromTrackerLog(string line)
        {
            if (!File.Exists(Properties.Settings.Default.filepath))
            {
                System.Windows.Forms.MessageBox.Show("Error: No tracker log found at the specified filepath.");
                return;
            }

            List<string> data = File.ReadAllLines(LogFilepath).ToList();
            data.RemoveAll(l => l == line);
            File.WriteAllLines(LogFilepath, data);
            FastRefreshTrackerLogViewer(data.ToArray());
        }

        private void addItemLine_Click(object sender, EventArgs e)
        {
            AddToTrackerLog($"ITEM --- {{{itemSelectBox.SelectedItem}}} at {{{locationSelectBox.SelectedItem}}}");
        }

        private void addTransitionLine_Click(object sender, EventArgs e)
        {
            if (areaRandomizer)
            {
                AddToTrackerLog($"TRANSITION --- {{{sourceTransitionSelect.SelectedItem.ToString()}}}-->{{{targetTransitionSelect.SelectedItem.ToString()}}}",
                    $"                ({transitionToArea[sourceTransitionSelect.SelectedItem.ToString()]} to {transitionToArea[sourceTransitionSelect.SelectedItem.ToString()]})");
            }
            else
            {
                AddToTrackerLog($"TRANSITION --- {{{sourceTransitionSelect.SelectedItem.ToString()}}}-->{{{targetTransitionSelect.SelectedItem.ToString()}}}");
            }
        }

        private void removeLineButton_Click(object sender, EventArgs e)
        {
            RemoveFromTrackerLog(trackerLogViewer.SelectedItem.ToString());
        }
        #endregion

        #region Settings
        public static string LogFilepath
        {
            get
            {
                if (Properties.Settings.Default.filepath is string path && File.Exists(path))
                {
                    return Properties.Settings.Default.filepath;
                }
                else
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Team Cherry\Hollow Knight\RandomizerTrackerLog.txt";
                    LogFilepath = path;
                    return path;
                }
            }
            set
            {
                Properties.Settings.Default.filepath = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string helperFilepath
        {
            get
            {
                if (Properties.Settings.Default.helperfilepath is string path && File.Exists(path))
                {
                    return Properties.Settings.Default.helperfilepath;
                }
                else
                {
                    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Team Cherry\Hollow Knight\RandomizerHelperLog.txt";
                    helperFilepath = path;
                    return path;
                }
            }
            set
            {
                Properties.Settings.Default.helperfilepath = value;
                Properties.Settings.Default.Save();
            }
        }

        private void filepathBox_TextChanged(object sender, EventArgs e)
        {
            LogFilepath = filepathBox.Text;
        }
        public string vertAlgo => (string)listBoxVertexAlgo.SelectedItem;
        public string edgeAlgo => (string)listBoxEdgeAlgo.SelectedItem;
        #endregion

        private void zoomToVertexButton_Click(object sender, EventArgs e)
        {
            var kvp = graph.VertexList.FirstOrDefault(v => v.Key.Name == (string)zoomToVertexSelect.SelectedItem);
            if (kvp.Value is VertexControl vertex)
            {
                System.Windows.Point pos = vertex.GetPosition();
                var rect = new Rect(pos.X - 150f, pos.Y - 150f, vertex.ActualWidth + 300f, vertex.ActualHeight + 450f);
                zoom.ZoomToContent(rect);
            }
        }

        private void toggleBenchesButton_click(object sender, EventArgs e)
        {
            Grapher.ToggleBenches(graph);
            if (Grapher.highlightMode == 1)
            {
                toggleBenchesButton.Text = "Hide Benches";
                helperButton.Text = "Show Helper Locations";
            }
            else
            {
                toggleBenchesButton.Text = "Show Benches";
            }
        }

        private void helperlogPath_textChanged(object sender, EventArgs e)
        {
            helperFilepath = helperlogBox.Text;
        }

        private void toggleHelperButton_click(object sender, EventArgs e)
        {
            Grapher.ToggleHelperLoc(graph);
            if (Grapher.highlightMode == 2)
            {
                helperButton.Text = "Hide Helper Locations";
                toggleBenchesButton.Text = "Show Benches";
            }
            else
            {
                helperButton.Text = "Show Helper Locations";
            }
        }

        private void roomInfoButton_click(object sender, EventArgs e)
        {
            string TransitionText = "Transitions:";
            bool usingTransitions = false;

            if (areaRandomizer)
            {
                foreach (string transition in areaTransitions)
                {
                    if (roomInfoBox.Text == transitionToArea[transition])
                    {
                        usingTransitions = true;
                        if (FoundOneWay.TryGetValue(transition, out string transentrance))
                        {
                            TransitionText += string.Format("\n{0} <--- {1}", transition, transentrance);
                            string inroom = transitionToRoom[transition];
                            if (transitionToRoom.TryGetValue(transentrance, out string outroom))
                            {
                                string incleanname = AltRoomNames.TryGetValue(inroom, out string altname) ? altname : inroom;
                                string outcleanname = AltRoomNames.TryGetValue(outroom, out string altnameout) ? altnameout : outroom;
                                if (incleanname != inroom && outcleanname != outroom)
                                {
                                    TransitionText += string.Format("\n    ({0} <--- {1})", incleanname, outcleanname);
                                }
                            }
                        }
                        else if (FoundTransitions.TryGetValue(transition, out string transexit))
                        {
                            TransitionText += string.Format("\n{0} ---> {1}", transition, transexit);
                            string inroom = transitionToRoom[transition];
                            if (transitionToRoom.TryGetValue(transexit, out string outroom))
                            {
                                string incleanname = AltRoomNames.TryGetValue(inroom, out string altname) ? altname : inroom;
                                string outcleanname = AltRoomNames.TryGetValue(outroom, out string altnameout) ? altnameout : outroom;
                                if (incleanname != inroom && outcleanname != outroom)
                                {
                                    TransitionText += string.Format("\n    ({0} ---> {1})", incleanname, outcleanname);
                                }
                            }
                        }
                        else
                        {
                            TransitionText += string.Format("\n{0} --- ???", transition);
                            string inroom = transitionToRoom[transition];
                            string incleanname = AltRoomNames.TryGetValue(inroom, out string altname) ? altname : inroom;
                            if (incleanname != inroom)
                            {
                                TransitionText += string.Format("\n    ({0})", incleanname);
                            }
                        }
                    }
                }
                int allitems = 0;
                int checkeditems = 0;
                foreach (string itemname in randomizedItems)
                {
                    if (roomInfoBox.Text == itemToArea[itemname] && !checkIfShopItem[itemname])
                    {
                        allitems += 1;
                        if (checkedLocations.Contains(itemname))
                        {
                            checkeditems += 1;
                        }
                    }
                }
                if (allitems > 0)
                {
                    TransitionText += string.Format("\n\nChecked {0} of {1} locations in this area", checkeditems, allitems);
                }
            }
            else if (roomRandomizer)
            {
                foreach (string transition in roomTransitions)
                {
                    if (roomInfoBox.Text == transitionToRoom[transition])
                    {
                        usingTransitions = true;
                        if (FoundOneWay.TryGetValue(transition, out string transentrance))
                        {
                            TransitionText += string.Format("\n{0} <--- {1}", transition, transentrance);
                            if (transitionToRoom.TryGetValue(transentrance, out string outroom))
                            {
                                if (AltRoomNames.TryGetValue(outroom, out string altnameout) && altnameout != outroom)
                                {
                                    TransitionText += string.Format("    ({0})", altnameout);
                                }
                            }
                        }
                        else if (FoundTransitions.TryGetValue(transition, out string transexit))
                        {
                            TransitionText += string.Format("\n{0} ---> {1}", transition, transexit);
                            if (transitionToRoom.TryGetValue(transexit, out string outroom))
                            {
                                if (AltRoomNames.TryGetValue(outroom, out string altnameout) && altnameout != outroom)
                                {
                                    TransitionText += string.Format("    ({0})", altnameout);
                                }
                            }
                        }
                        else
                        {
                            TransitionText += string.Format("\n{0} --- ???", transition);
                        }
                    }
                }
                int allitems = 0;
                int checkeditems = 0;
                foreach (string itemname in randomizedItems)
                {
                    if (roomInfoBox.Text == itemToRoom[itemname] && !checkIfShopItem[itemname])
                    {
                        allitems += 1;
                        if (checkedLocations.Contains(itemname))
                        {
                            checkeditems += 1;
                        }
                    }
                }
                if (allitems > 0)
                {
                    TransitionText += string.Format("\n\nChecked {0} of {1} locations in this room", checkeditems, allitems);
                }
            }



            roomStatsBox.Text = string.Empty;
            if (usingTransitions)
            {
                roomStatsBox.Text += TransitionText;
            }
        }
    }
}
