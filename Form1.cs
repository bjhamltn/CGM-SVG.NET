/*
 * Copyright (C) Benjamin Hamilton 2016 
 * Program to covert computer graphic metafile (CGM) into scalable vector graphics (SVG)
 */

/* 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using BitMiracle.LibTiff.Classic;
using BitMiracle.LibTiff;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;




#region JCGM: Used for testing and troble shooting and is not required by the converter.
using net.sf.jcgm.core;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;  
#endregion

namespace cgm_decoder
{



    public partial class Form1 : System.Windows.Forms.Form
    {
        
        public int picture_idx = 0;
        public int vdc_idx = 0;
        public bool paramLen_rollover = false;
        #region debug enable console write line of decoded metafile name
        string filename = "fim02"; //plgset01
        bool debug = false;
        bool altSet = true; 
        #endregion
        public Form1()
        {
            #region Sets the location of the file being converted and the location of the output file.
            string cgmfilename_in = @"C:\Users\795627\Downloads\Reference Files\ata30-cgms\" + filename + ".cgm";
            string cgmfilename_out = @"C:\Users\795627\Desktop\" + filename + ".txt";
            if (altSet)
            {
                cgmfilename_in = @"C:\Users\795627\Desktop\CGM\" + filename + ".cgm";
                cgmfilename_out = @"C:\Users\795627\Desktop\" + filename + ".txt";
            }
            #endregion

            InitializeComponent();

            #region Default line types for SVG dash arrays
            lineEdgeTypeLookUp.Add(new LineEdgeType()
            {
                id = 1,
                dashseq = (new float[] { 2, 0 }).ToList()
            });

            lineEdgeTypeLookUp.Add(new LineEdgeType()
            {
                id = 2,
                dashseq = (new float[] { 2, 2 }).ToList()
            });

            lineEdgeTypeLookUp.Add(new LineEdgeType()
            {
                id = 3,
                dashseq = (new float[] { 0.001f, 2 }).ToList()
            });

            lineEdgeTypeLookUp.Add(new LineEdgeType()
            {
                id = 4,
                dashseq = (new float[] { 2, 2, 0.001f, 2 }).ToList()
            });

            lineEdgeTypeLookUp.Add(new LineEdgeType()
            {
                id = 5,
                dashseq = (new float[] { 2, 2, 0.001f, 2, 0.001f, 2 }).ToList()
            });
            #endregion



            #region JCGM CGM used for testing and tunning
            java.io.File cgmFile = new java.io.File(cgmfilename_in);
            java.io.DataInputStream input = new java.io.DataInputStream(new java.io.FileInputStream(cgmFile));
            net.sf.jcgm.core.CGM cgm = new CGM();
            cgm.read(input);
            List<Command> commands = cgm.getCommands().toArray().Cast<Command>().ToList();
            StreamWriter sw = new StreamWriter(cgmfilename_out, false);
            foreach (Command cmd in commands)
            {
                sw.WriteLine(cmd.toString());
            }
            sw.Close();
            #endregion

            BinaryReader br = new BinaryReader(new FileStream(cgmfilename_in, FileMode.Open, FileAccess.Read));

            picture_idx = 0;
            vdc_idx = 0;
            int paramLen = 0;
            byte elemclass = 0;
            byte elemId = 0;
            string elemName = "";
            byte[] buffer = new byte[0];
            int bytesread = 0;
            List<Cgm_Element> Cgm_Elements = new List<Cgm_Element>();

            getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);

            while (bytesread > 0)
            {
                //if (elemName != "")
                if (!string.IsNullOrEmpty(elemName))
                {
                    parseMetaElement(ref br, ref buffer, ref bytesread, ref paramLen, ref elemclass, ref elemId, ref elemName, ref Cgm_Elements, true);
                }
                else
                {
                    getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);
                }
            }
            br.Close();

            CGM_t_SVG(Cgm_Elements);
        }


        public XmlDocument createSVGTemplate(List<Cgm_Element> Cgm_Elements)
        {
            #region Width Height
            XmlDocument cgm_svg = new XmlDocument();

            XmlNode path = cgm_svg.CreateElement("path");

            if (Cgm_Elements.Where(fd => fd.page_height > 0 && fd.page_width > 0).Count() == 0)
            {
                Cgm_Elements[0].page_height = 1;
                Cgm_Elements[0].page_width = 1;
            }

            Cgm_Element bgColorElem = Cgm_Elements.FirstOrDefault(fd => fd.elem_Name == "BACKGROUND COLOUR");


            Cgm_Element dd = Cgm_Elements.First(fd => fd.page_height > 0 && fd.page_width > 0);

            cgm_svg.LoadXml(String.Format("<svg  height=\"100%\" width=\"100%\" viewBox =\" {2} {2} {3} {4} \"/>",
                Math.Max(100, dd.page_height * dd.scaleFactor),
                Math.Max(100, dd.page_width * dd.scaleFactor),
                0,
                dd.page_width,
                dd.page_height
                ));
            if (bgColorElem != null)
            {
                XmlNode background = cgm_svg.CreateElement("rect");

                background.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = "background";
                background.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = "0";
                background.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = "0";
                background.Attributes.Append(cgm_svg.CreateAttribute("y")).Value = "0";
                background.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = dd.page_width.ToString();
                background.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = dd.page_height.ToString();
                background.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("fill:#{0}", bgColorElem.bgColor.Name.Substring(2));
                cgm_svg.DocumentElement.AppendChild(background);
            }
            float y_offset = dd.vdcExtent == null ? 0 : dd.page_height - dd.vdcExtent[1].Y;
            #endregion

            if (dd.isbottomUp)
            {
                Cgm_Elements.ForEach(delegate(Cgm_Element cgm)
                {
                    #region MyRegion
                    cgm.page_height = dd.true_height;
                    if (cgm.elem_Name.EndsWith("TEXT"))
                    {
                        cgm.position.Y = dd.page_height - (cgm.position.Y + cgm.characterHeight) - y_offset;

                    }

                    else if (cgm.elem_Name.StartsWith("ELLIPTICAL ARC"))
                    {


                        PointF p_start = PointF.Add(cgm.points[0], new SizeF(cgm.points[3].X, cgm.points[3].Y));
                        PointF p_endin = PointF.Add(cgm.points[0], new SizeF(cgm.points[4].X, cgm.points[4].Y));

                        PointF center = PointF.Subtract(new PointF(cgm.points[0].X, dd.page_height), new SizeF(0, cgm.points[0].Y));

                        p_start.Y = dd.page_height - p_start.Y;

                        p_endin.Y = dd.page_height - p_endin.Y;

                        PointF p_start_alt = PointF.Subtract(p_start, new SizeF(center.X, center.Y));
                        PointF p_endin_alt = PointF.Subtract(p_endin, new SizeF(center.X, center.Y));
                        cgm.points.Add(cgm.points[1]);
                        cgm.points.Add(cgm.points[2]);
                        cgm.points.Add(cgm.points[3]);
                        cgm.points.Add(cgm.points[4]);
                        cgm.points = cgm.points.Select((fd, idx) => idx <= 2 ? new PointF(fd.X, dd.page_height - fd.Y - y_offset) : fd).ToList();
                        cgm.points[3] = p_start_alt;
                        cgm.points[4] = p_endin_alt;
                    }
                    else
                    {

                        cgm.points = cgm.points.Select(fd => new PointF(fd.X, dd.page_height - fd.Y - y_offset)).ToList();
                    }

                    #endregion
                });
            }



            if (!dd.isleftRight)
            {
                Cgm_Elements.ForEach(delegate(Cgm_Element cgm)
                {
                    #region MyRegion
                    cgm.page_width = dd.page_width;
                    if (cgm.elem_Name.EndsWith("TEXT"))
                    {
                        cgm.position.X = dd.page_width - cgm.position.X;
                    }

                    else if (cgm.elem_Name.StartsWith("ELLIPTICAL ARC"))
                    {


                        PointF p_start = PointF.Add(cgm.points[0], new SizeF(cgm.points[3].X, cgm.points[3].Y));
                        PointF p_endin = PointF.Add(cgm.points[0], new SizeF(cgm.points[4].X, cgm.points[4].Y));

                        PointF center = PointF.Subtract(new PointF(dd.page_width, cgm.points[0].Y), new SizeF(cgm.points[0].X, 0));

                        p_start.X = dd.page_width - p_start.X;

                        p_endin.X = dd.page_width - p_endin.X;

                        PointF p_start_alt = PointF.Subtract(p_start, new SizeF(center.X, center.Y));
                        PointF p_endin_alt = PointF.Subtract(p_endin, new SizeF(center.X, center.Y));
                        cgm.points.Add(cgm.points[1]);
                        cgm.points.Add(cgm.points[2]);
                        cgm.points.Add(cgm.points[3]);
                        cgm.points.Add(cgm.points[4]);
                        cgm.points = cgm.points.Select((fd, idx) => idx <= 2 ? new PointF(dd.page_width - fd.X, fd.Y) : fd).ToList();
                        cgm.points[3] = p_start_alt;
                        cgm.points[4] = p_endin_alt;
                    }
                    else
                    {
                        cgm.points = cgm.points.Select(fd => new PointF(dd.page_width - fd.X, fd.Y)).ToList();
                    }

                    #endregion
                });
            }
            return cgm_svg;
        }
        
        public void CGM_t_SVG(List<Cgm_Element> Cgm_Elements)
        {
            IEnumerable<IGrouping<int, Cgm_Element>> vdcExtents = Cgm_Elements.GroupBy(fd => fd.vdc_idx);

            foreach (IGrouping<int, Cgm_Element> vdcExtentGroup in vdcExtents)
            {
                List<Cgm_Element> ll = vdcExtentGroup.Cast<Cgm_Element>().ToList();
                createSVG(ll, vdcExtentGroup.Key);
            }
        }
        
        public void createSVG(List<Cgm_Element> Cgm_Elements, int vdcIdx)
        {

            bool pathNew = true;

            XmlDocument cgm_svg = createSVGTemplate(Cgm_Elements);
            XmlNode path = cgm_svg.CreateElement("path");
            Cgm_Element prevCgm = new Cgm_Element();
            int cgm_idx = 0;
            int cgm_idx_abs = 0;
            cgm_svg.DocumentElement.SetAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
            cgm_svg.DocumentElement.SetAttribute("xmlns:dc", "http://purl.org/dc/elements/1.1/");
            cgm_svg.DocumentElement.SetAttribute("xmlns:cc", "http://creativecommons.org/ns#");
            cgm_svg.DocumentElement.SetAttribute("xmlns:rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            cgm_svg.DocumentElement.SetAttribute("xmlns:svg", "http://www.w3.org/2000/svg");
            
            XmlNode defs = cgm_svg.DocumentElement.AppendChild(cgm_svg.CreateElement("defs"));

            createImagePattern(cgm_svg, Cgm_Elements, 1f);

            Enumerable.Range(1, 6).ToList().ForEach(delegate(int hash_id)
            {
                #region MyRegion
                String hatch_id = String.Format("hashtype_{0}", hash_id);

                XmlDocument hatch = new XmlDocument();

                byte[] data = (byte[])cgm_struct.ResourceManager.GetObject(hatch_id);

                hatch.LoadXml(System.Text.Encoding.Default.GetString(data));

                XmlNamespaceManager xml = new XmlNamespaceManager(hatch.NameTable);
                xml.AddNamespace("space", "http://www.w3.org/2000/svg");
                XmlNode pattern = null;

                pattern = hatch.DocumentElement.SelectSingleNode("//space:path", xml);
                if (pattern == null)
                {
                    pattern = hatch.DocumentElement.SelectSingleNode("//space:polygon", xml);
                }

                XmlNode hatchpattern = cgm_svg.CreateElement("pattern");
                hatchpattern.InnerXml = pattern.OuterXml.Replace("xmlns=\"http://www.w3.org/2000/svg\"", "");

                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("id")).Value = hatch_id;
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("patternUnits")).Value = "userSpaceOnUse";
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = "1";
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = "1";


                defs.AppendChild(hatchpattern);
                #endregion
            });
            int pic_idx = 0;
            int lstSheet = Cgm_Elements.Last().picture_idx;
            string template = cgm_svg.OuterXml;
            cgm_svg.DocumentElement.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
            foreach (Cgm_Element cgmElement in Cgm_Elements)
            {
                if (cgmElement.elem_Name == ("END PICTURE"))
                {
                    Console.WriteLine("Start New Sheet");
                }
                pathNew = true;
                bool close = cgmElement.isFig;
                bool isFigure = cgmElement.isFig;
                if (pic_idx != cgmElement.picture_idx)
                {                    
                    cgm_svg.Save(String.Format(@"C:\Users\795627\Desktop\cmg_svg_{0}_{1}.svg", vdcIdx.ToString(), pic_idx.ToString()));
                    cgm_svg = new XmlDocument();
                    cgm_svg.LoadXml(template);                    
                    cgm_svg.DocumentElement.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
                    cgm_idx = 0;
                }
                pic_idx = cgmElement.picture_idx;
                #region SVG Elements
                if (cgmElement.elem_Name == ("POLYLINE"))
                {
                    #region MyRegion
                    int pt_idx = 0;
                    StringBuilder polyline = new StringBuilder();

                    polyline.Append(" ");

                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    }

                    foreach (PointF pt in cgmElement.points)
                    {

                        if (pt_idx == 0 && isFigure && path.Attributes["d"].Value != "")
                        {
                            if (path.Attributes["d"].Value.Trim().EndsWith("M"))
                            {
                                polyline.Append(String.Format(" {0} {1} ", pt.X, pt.Y));
                            }
                            else
                            {
                                polyline.Append(String.Format("L {0} {1} ", pt.X, pt.Y));
                            }

                        }
                        else if (pt_idx == 0)
                        {
                            polyline.Append(String.Format("M {0} {1} L ", pt.X, pt.Y));
                        }
                        else
                        {
                            polyline.Append(String.Format("{0} {1} ", pt.X, pt.Y));
                        }
                        pt_idx++;
                        path.Attributes["d"].Value += polyline.ToString();
                        polyline = new StringBuilder();
                    }


                    #endregion
                }
                else if (cgmElement.elem_Name == "CELL ARRAY")
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("image");
                    cgm_svg.DocumentElement.AppendChild(path);
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = (cgmElement.points[0].X).ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("y")).Value = (cgmElement.points[0].Y).ToString();

                    path.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = cgmElement.rasterImage.Width.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = cgmElement.rasterImage.Height.ToString();
                    float scaleX = 1;
                    float scaleY = 1;
                    if (cgmElement.points[0].X > cgmElement.points[1].X)
                    {
                        scaleX = -1;
                        path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = (-cgmElement.points[0].X).ToString();
                    }
                    if (cgmElement.points[0].Y > cgmElement.points[1].Y)
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = (-cgmElement.points[0].Y).ToString();
                        scaleY = -1;
                    }
                    scaleY = scaleY * cgmElement.imageScale;
                    scaleX = scaleX * cgmElement.imageScale;
                    path.Attributes.Append(cgm_svg.CreateAttribute("transform")).Value = String.Format("scale({0},{1}) ", scaleX, scaleY,
                        cgmElement.imageRotation, cgmElement.rasterImage.Width, cgmElement.rasterImage.Height, cgmElement.imageScale_delta_x, cgmElement.imageScale_delta_y);


                    path.Attributes.Append(cgm_svg.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink")).Value = string.Format("data:image/bmp;base64,{0}", cgmElement.raster2base64());

                    #endregion
                }
                else if (cgmElement.elem_Name == "BITONAL TILE")
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("image");
                    cgm_svg.DocumentElement.AppendChild(path);
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = cgmElement.beginTilePoint.X.ToString();

                    path.Attributes.Append(cgm_svg.CreateAttribute("y")).Value = (cgmElement.page_height - cgmElement.beginTilePoint.Y).ToString();

                    path.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = (cgmElement.rasterImage.Width * cgmElement.imageScale_x).ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = (cgmElement.rasterImage.Height * cgmElement.imageScale_y).ToString();

                    path.Attributes.Append(cgm_svg.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink")).Value = string.Format("data:image/bmp;base64,{0}", cgmElement.raster2base64());

                    #endregion
                }
                else if (cgmElement.elem_Name == ("DISJOINT POLYLINE"))
                {
                    #region MyRegion
                    int pt_idx = 0;
                    StringBuilder polyline = new StringBuilder();

                    polyline.Append(" ");

                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    }

                    foreach (PointF pt in cgmElement.points)
                    {

                        if (pt_idx == 0)
                        {
                            polyline.Append(String.Format("M {0} {1} ", pt.X, pt.Y));
                        }
                        else
                        {
                            polyline.Append(String.Format("{0} {1} ", pt.X, pt.Y));
                        }
                        if (pt_idx == 1)
                        {
                            pt_idx = 0;
                        }
                        else
                        {
                            pt_idx++;
                        }
                        path.Attributes["d"].Value += polyline.ToString();
                        polyline = new StringBuilder();
                    }


                    #endregion
                }
                else if (cgmElement.elem_Name == ("POLYBEZIER"))
                {
                    #region MyRegion
                    int pt_idx = 0;
                    StringBuilder polybezier = new StringBuilder();

                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    }
                    bool continous = cgmElement.isContinuous();
                    foreach (PointF pt in cgmElement.points)
                    {
                        if (pt_idx == 0 && isFigure && path.Attributes["d"].Value != "")
                        {
                            if (continous)
                            {
                                polybezier.Append(String.Format("L {0} {1} C ", pt.X, pt.Y));
                            }
                            else
                            {
                                polybezier.Append(String.Format("M {0} {1} C ", pt.X, pt.Y));
                            }
                        }
                        else if (pt_idx == 0)
                        {
                            polybezier.Append(String.Format("M {0} {1} C ", pt.X, pt.Y));
                        }
                        else
                        {
                            polybezier.Append(String.Format("{0} {1} ", pt.X, pt.Y));
                        }


                        if (continous)
                        {
                            pt_idx++;
                        }
                        else if (!continous && pt_idx == 3)
                        {
                            pt_idx = 0;
                        }
                        else
                        {
                            pt_idx++;
                        }

                        path.Attributes["d"].Value += polybezier.ToString();
                        polybezier = new StringBuilder();

                    }

                    #endregion
                }
                else if (cgmElement.elem_Name == ("POLYGON SET"))
                {
                    #region MyRegion
                    int pt_idx = 0;
                    int nwPtIdx = 0;
                    StringBuilder polyset = new StringBuilder();
                    PointF closePnt = cgmElement.points.First();
                    List<PointF> pontBackList = new List<PointF>();
                    List<LineSegment> segmentSet = new List<LineSegment>();
                    LineSegment lineSegment = new LineSegment();
                    path = cgm_svg.CreateElement("path");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                    foreach (PointF pt in cgmElement.points)
                    {
                        switch (cgmElement.polygonSetFlags[pt_idx])
                        {
                            case "invisible":

                                if (nwPtIdx == 0)
                                {
                                    polyset.Append(String.Format(" M {0} {1} ", pt.X, pt.Y));
                                    lineSegment = new LineSegment();
                                    lineSegment.p1 = pt;
                                    pontBackList = new List<PointF>();
                                }
                                else
                                {
                                    polyset.Append(String.Format(" {0} {1} M ", pt.X, pt.Y));
                                    lineSegment.p2 = pt;
                                    segmentSet.Add(lineSegment);
                                    lineSegment = new LineSegment();
                                }

                                break;
                            case "visible":
                                if (nwPtIdx == 0)
                                {
                                    polyset.Append(String.Format("M {0} {1} ", pt.X, pt.Y));
                                    lineSegment = new LineSegment();
                                    lineSegment.p1 = pt;
                                    pontBackList = new List<PointF>();
                                }
                                else
                                {
                                    polyset.Append(String.Format(" {0} {1} ", pt.X, pt.Y));
                                    lineSegment.p2 = pt;
                                    segmentSet.Add(lineSegment);
                                    lineSegment = new LineSegment();
                                }
                                break;
                            case "close,invisible":
                                if (nwPtIdx == 0)
                                {
                                    polyset.Append(String.Format(" M {0} {1} ", pt.X, pt.Y));
                                }
                                else
                                {
                                    closePnt = pontBackList.First();
                                    string polyFrag = polyset.ToString().TrimEnd(new[] { ' ', 'M' });
                                    polyset = new StringBuilder();
                                    polyset.Append(polyFrag);
                                    polyset.Append(String.Format(" {0} {1} M {2} {3} ", pt.X, pt.Y, closePnt.X, closePnt.Y));
                                    
                                    nwPtIdx = -1;
                                    segmentSet = new List<LineSegment>();
                                    pontBackList = new List<PointF>();
                                }
                                break;
                            case "close,visible":
                                if (nwPtIdx == 0)
                                {

                                    polyset.Append(String.Format("M {0} {1} Z ", pt.X, pt.Y));
                                    pontBackList = new List<PointF>();
                                }
                                else
                                {
                                    closePnt = pontBackList.First();
                                    string polyFrag = polyset.ToString().TrimEnd(new[] { ' ', 'M' });
                                    polyset = new StringBuilder();
                                    polyset.Append(polyFrag);
                                    polyset.Append(String.Format(" {0} {1} {2} {3} ", pt.X, pt.Y, closePnt.X, closePnt.Y));
                                    #region MyRegion
                                    //lineSegment.p1 = pt;
                                    //lineSegment.p2 = closePnt;
                                    //segmentSet.Add(lineSegment);

                                    //polyset = new StringBuilder();
                                    //polyset.Append("M ");
                                    //for (int sidx = 0; sidx < segmentSet.Count; )
                                    //{

                                    //    LineSegment seg = segmentSet[sidx];
                                    //    if (segmentSet.Last() != seg)
                                    //    {
                                    //        LineSegment segNx = segmentSet[sidx + 1];
                                    //        if (seg.p1.X == segNx.p2.X && seg.p1.Y == segNx.p2.Y)
                                    //        {
                                    //            segmentSet[sidx] = segNx;
                                    //            segmentSet[sidx + 1] = seg;
                                    //        }
                                    //    }
                                    //    sidx += 2;
                                    //}
                                    //for (int sidx = 0; sidx < segmentSet.Count; sidx++)
                                    //{
                                    //    LineSegment seg = segmentSet[sidx];
                                    //    polyset.Append(String.Format(" {0} {1} {2} {3} ", seg.p1.X, seg.p1.Y, seg.p2.X, seg.p2.Y));

                                    //}
                                    //path.Attributes["d"].Value += polyset.ToString();
                                    //polyset = new StringBuilder(); 
                                    #endregion
                                    nwPtIdx = -1;
                                    segmentSet = new List<LineSegment>();
                                    pontBackList = new List<PointF>();
                                }
                                break;
                        }
                        pontBackList.Add(pt);
                        pt_idx++;
                        nwPtIdx++;
                    }

                    
                    path.Attributes["d"].Value += polyset.ToString();
                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }
                else if (cgmElement.elem_Name == ("POLYGON"))
                {
                    #region MyRegion
                    int pt_idx = 0;
                    StringBuilder polyset = new StringBuilder();

                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                        cgm_svg.DocumentElement.AppendChild(path);
                    }

                    foreach (PointF pt in cgmElement.points)
                    {
                        if (pt_idx == 0 && path.Attributes["d"].Value == "")
                        {
                            polyset.Append(String.Format("M {0} {1} ", pt.X, pt.Y));
                        }
                        else if (pt_idx == 0 && path.Attributes["d"].Value.Trim().EndsWith("M") == false)
                        {
                            polyset.Append(String.Format(" M {0} {1} ", pt.X, pt.Y));
                        }
                        else
                        {
                            polyset.Append(String.Format(" {0} {1} ", pt.X, pt.Y));
                        }
                        pt_idx++;
                    }
                    polyset.Append(String.Format(" {0} {1} Z ", cgmElement.points.First().X, cgmElement.points.First().Y));

                    if (!isFigure)
                    {
                        path.Attributes["d"].Value = polyset.ToString();
                    }
                    else
                    {
                        path.Attributes["d"].Value += polyset.ToString();
                    }
                    #endregion
                }
                else if (cgmElement.elem_Name == ("ELLIPSE"))
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("ellipse");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("cx"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("cy"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("rx"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("ry"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("transform"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();


                    double angle2 = 0;
                    double angle = 0;
                    double rx;
                    double ry;



                    Cgm_Element ellispe = new Cgm_Element();
                    List<PointF> ellipsePoints = new List<PointF>();
                    ellispe.points.Add(cgmElement.points[0]);
                    ellispe.points.Add(cgmElement.points[1]);
                    ellispe.points.Add(cgmElement.points[2]);

                    getellipse(ellispe, out rx, out ry, out angle, out angle2);

                    string cx = path.Attributes["cx"].Value = cgmElement.points[0].X.ToString();
                    string cy = path.Attributes["cy"].Value = cgmElement.points[0].Y.ToString();
                    path.Attributes["transform"].Value = "rotate(" + (angle).ToString() + " " + cx + " " + cy + ")";
                    path.Attributes["rx"].Value = rx.ToString();
                    path.Attributes["ry"].Value = ry.ToString();



                    cgm_svg.DocumentElement.AppendChild(path);

                    #endregion
                }
                else if (cgmElement.elem_Name == "RECTANGLE")
                {
                    #region MyRegion
                    float x = Math.Min(cgmElement.points[0].X, cgmElement.points[1].X);
                    float y = Math.Min(cgmElement.points[0].Y, cgmElement.points[1].Y);

                    path = cgm_svg.CreateElement("rect");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = x.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("y")).Value = y.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = cgmElement.width.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = cgmElement.height.ToString();
                    cgm_svg.DocumentElement.AppendChild(path);

                    #endregion
                }
                else if (cgmElement.elem_Name == ("CIRCLE"))
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("circle");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("cx"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("cy"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("r"));

                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();

                    string cx = path.Attributes["cx"].Value = cgmElement.points[0].X.ToString();
                    string cy = path.Attributes["cy"].Value = cgmElement.points[0].Y.ToString();

                    path.Attributes["r"].Value = cgmElement.arc_radius.ToString();

                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }
                else if (cgmElement.elem_Name == "CIRCULAR ARC 3 POINT CLOSE")
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                    }
                    float m, b;
                    double angle_dir, angle_LEN, a, a2;
                    char path_len = '0';
                    char dir = '0';

                    distance_180(cgmElement.points[3], cgmElement.points[0], out  a, out m, out b);
                    distance_180(cgmElement.points[3], cgmElement.points[2], out  a2, out m, out b);

                    angle_LEN = angle_dir = a = a2 - a;


                    #region MyRegion
                    getArcParams_cir((float)angle_dir, out path_len, out dir);
                    #endregion

                    path.Attributes.Append(cgm_svg.CreateAttribute("a2")).Value = a2.ToString();
                    path.Attributes["d"].Value += String.Format(" M {0} {1} A ", cgmElement.points[0].X, cgmElement.points[0].Y);
                    path.Attributes["d"].Value += String.Format("{0} {0} {1} ", cgmElement.arc_radius, a);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", path_len, dir);
                    path.Attributes["d"].Value += String.Format("{0} {1}", cgmElement.points[2].X, cgmElement.points[2].Y);

                    if (cgmElement.cir_arc_closure == "pieclosure")
                    {
                        path.Attributes["d"].Value += String.Format("L {0} {1} ", cgmElement.points[3].X, cgmElement.points[3].Y);
                        path.Attributes["d"].Value += String.Format("{0} {1} Z", cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    else if (cgmElement.cir_arc_closure == "chord closure")
                    {
                        path.Attributes["d"].Value += String.Format("L {0} {1} Z", cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }
                else if (cgmElement.elem_Name == "CIRCULAR ARC 3 POINT")
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                    }
                    float m, b;
                    double angle_dir, a, a2;

                    distance_180(cgmElement.points[3], cgmElement.points[0], out  a, out m, out b);

                    distance_180(cgmElement.points[3], cgmElement.points[2], out  a2, out m, out b);

                    char path_len = '0';
                    char dir = '0';

                    angle_dir = a = a2 - a;

                    path.Attributes.Append(cgm_svg.CreateAttribute("a1")).Value = a.ToString();


                    #region MyRegion
                    getArcParams_cir((float)angle_dir, out path_len, out dir);
                    #endregion

                    path.Attributes.Append(cgm_svg.CreateAttribute("a2")).Value = a2.ToString();
                    path.Attributes["d"].Value += String.Format(" M {0} {1} A ", cgmElement.points[0].X, cgmElement.points[0].Y);
                    path.Attributes["d"].Value += String.Format("{0} {0} {1} ", cgmElement.arc_radius, a);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", path_len, dir);
                    path.Attributes["d"].Value += String.Format("{0} {1}", cgmElement.points[2].X, cgmElement.points[2].Y);
                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }
                else if (cgmElement.elem_Name == ("CIRCULAR ARC CENTRE"))
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                    }
                    float m, b;
                    double a, a2;
                    char path_len = '0';
                    char dir = '0';

                    distance_180(cgmElement.points[0], cgmElement.points[1], out  a, out m, out b);
                    distance_180(cgmElement.points[0], cgmElement.points[2], out  a2, out m, out b);

                    a = a2 - a;

                    #region MyRegion
                    getArcParams_cir((float)a, out path_len, out dir);
                    #endregion

                    if (path.Attributes["d"].Value.EndsWith("M") == false)
                    {
                        path.Attributes["d"].Value += String.Format(" M {0} {1} A ", cgmElement.points[1].X, cgmElement.points[1].Y);
                    }
                    else
                    {
                        path.Attributes["d"].Value += String.Format(" {0} {1} A ", cgmElement.points[1].X, cgmElement.points[1].Y);
                    }


                    path.Attributes["d"].Value += String.Format("{0} {0} {1} ", cgmElement.arc_radius, a);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", path_len, dir);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", cgmElement.points[2].X, cgmElement.points[2].Y);
                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }
                else if (cgmElement.elem_Name == "CIRCULAR ARC CENTRE CLOSE")
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                    }
                    float m, b;
                    double a, a2;
                    distance_180(cgmElement.points[0], cgmElement.points[1], out  a, out m, out b);
                    distance_180(cgmElement.points[0], cgmElement.points[2], out  a2, out m, out b);

                    char path_len = '0';
                    char dir = '0';
                    double angle_dir = a = a2 - a;

                    #region MyRegion
                    getArcParams_cir((float)angle_dir, out path_len, out dir);
                    #endregion


                    path.Attributes["d"].Value += String.Format(" M {0} {1} A ", cgmElement.points[1].X, cgmElement.points[1].Y);
                    path.Attributes["d"].Value += String.Format("{0} {0} {1} ", cgmElement.arc_radius, a);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", path_len, dir);
                    path.Attributes["d"].Value += String.Format("{0} {1} ", cgmElement.points[2].X, cgmElement.points[2].Y);

                    if (cgmElement.cir_arc_closure == "pieclosure")
                    {
                        path.Attributes["d"].Value += String.Format("L {0} {1} ", cgmElement.points[0].X, cgmElement.points[0].Y);
                        path.Attributes["d"].Value += String.Format("{0} {1} Z", cgmElement.points[1].X, cgmElement.points[1].Y);
                    }
                    else if (cgmElement.cir_arc_closure == "chord closure")
                    {
                        path.Attributes["d"].Value += String.Format("L {0} {1} Z", cgmElement.points[1].X, cgmElement.points[1].Y);
                    }
                    cgm_svg.DocumentElement.AppendChild(path);
                    #endregion
                }               
                else if (cgmElement.elem_Name.StartsWith("ELLIPTICAL ARC"))
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    }

                    path.Attributes.Append(cgm_svg.CreateAttribute("delta_start"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("delta_end"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("p1"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("p2"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("cx"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("cy"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("ry"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("rx"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("center"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("angle_dir"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("angle_ends"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("angle_len"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("angle_DIR"));
                    string cx = cgmElement.points[0].X.ToString();
                    string cy = cgmElement.points[0].Y.ToString();




                    path.Attributes["delta_start"].Value = cgmElement.points[3].ToString();
                    path.Attributes["delta_end"].Value = cgmElement.points[4].ToString();
                    path.Attributes["p1"].Value = cgmElement.points[1].ToString();
                    path.Attributes["p2"].Value = cgmElement.points[2].ToString();
                    path.Attributes["center"].Value = cgmElement.points[0].ToString();


              
                    char dir = '0';
                    char path_len = '0';
                    string angle_s = "0";
                    double angle = 0;
                    double angle_LEN = 0;
                    double angle_dir = 0;
                    double angle_3 = 0;
                    double ry = 0;
                    double rx = 0;

                    

                    /////////////////////////////////////////////////
                    Cgm_Element ellispe = new Cgm_Element();
                    List<PointF> ellipsePoints = new List<PointF>();
                    ellispe.points.Add(cgmElement.points[0]);
                    ellispe.points.Add(cgmElement.points[1]);
                    ellispe.points.Add(cgmElement.points[2]);

                    getellipse(ellispe, out rx, out ry, out angle, out angle_dir);
                    angle_s = (360 + angle).ToString();
                    ///////////////////////////////////////////////////////
                    #region MyRegion
                    XmlNode ee = cgm_svg.CreateElement("ellipse");
                    ee.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    ee.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    ee.Attributes.Append(cgm_svg.CreateAttribute("d"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("transform"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke-width"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("fill"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("style"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("delta_start"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("delta_end"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("p1"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("p2"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("cx"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("cy"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("ry"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("rx"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("center"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("angle_s"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("angle_ends"));
                    ee.Attributes.Append(cgm_svg.CreateAttribute("angle_len"));
                    
                    ee.Attributes["cx"].Value = cgmElement.points[0].X.ToString();
                    ee.Attributes["cy"].Value = cgmElement.points[0].Y.ToString();
                    ee.Attributes["transform"].Value = "rotate(" + angle.ToString() + " " + cx + " " + cy + ")";
                    ee.Attributes["rx"].Value = rx.ToString();
                    ee.Attributes["ry"].Value = ry.ToString();
                    ee.Attributes.Append(cgm_svg.CreateAttribute("fill")).Value = "none";
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke")).Value = "black";
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke-width")).Value = "0.1";
                    //cgm_svg.DocumentElement.AppendChild(ee);
                    #endregion
                    ///////////////////////////////////////////////////////
                    float slope_p = 0;
                    float bint_p = 0;
                    double angle_P1 = 0;
                    double angle_P2 = 0;
                    double angle_DIR = 0;

                    PointF p_start = PointF.Subtract(cgmElement.points[1], new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
                    PointF p_end = PointF.Subtract(cgmElement.points[2], new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
                    distance_180_xrs(p_end, p_start, out angle_P1, out angle_LEN, out slope_p, out bint_p);

                    p_start = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[3].X, cgmElement.points[3].Y));
                    p_end = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[4].X, cgmElement.points[4].Y));

                    distance_180(cgmElement.points[0], p_start, out angle_P1, out slope_p, out bint_p);
                    PointF diff = PointF.Subtract(cgmElement.points[0], new SizeF(p_start.X, p_start.Y));
                    if (rx == ry && rx == 0)
                    {
                        p_start = new PointF(cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    else if (diff.X == 0 && diff.Y == 0)
                    {
                        p_start = new PointF(cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    else
                    {
                        
                        p_start = finfPontOnElispe((float)(angle), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_start.X, p_start.Y, float.IsInfinity(slope_p) ? p_start.Y : p_start.X);
                        
                        
                    }
                    


                    distance_180(cgmElement.points[0], p_end, out angle_P2, out slope_p, out bint_p);
                    diff = PointF.Subtract(cgmElement.points[0], new SizeF(p_end.X, p_end.Y));
                    if (rx == ry && rx == 0)
                    {
                        p_end = new PointF(cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    else if (diff.X == 0 && diff.Y == 0)
                    {
                        p_end = new PointF(cgmElement.points[0].X, cgmElement.points[0].Y);
                    }
                    else
                    {
                        p_end = finfPontOnElispe((float)((angle)), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_end.X, p_end.Y, float.IsInfinity(slope_p) ? p_end.Y : p_end.X);                                                
                    }
                    
                    


                    PointF pA1 = PointF.Subtract(p_start, new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));

                    PointF pA2 = PointF.Subtract(p_end, new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
                    
                    distance_180_xrs(pA2, pA1, out angle_P1, out angle_DIR, out slope_p, out bint_p);
                    
                    //Console.WriteLine(angle_DIR + "  " + Math.Round(angle_dir, 0) + " " + angle_LEN);



                    //angle_DIR = angle_DIR < 0 ? angle_DIR + 360 : angle_DIR;
                    getArcParams_ell_xrs((float)angle_DIR, (float)angle_dir, (float)angle_LEN, out path_len, out dir);
                    path.Attributes["angle_dir"].Value = Math.Round(angle_dir, 2).ToString();
                    path.Attributes["angle_ends"].Value = Math.Round(angle_3, 2).ToString();
                    path.Attributes["angle_len"].Value = Math.Round(angle_LEN, 2).ToString();
                    path.Attributes["angle_DIR"].Value = Math.Round(angle_DIR, 2).ToString();
                    bool shareLine = false;

                    PointF connectedPt = new PointF();
                    if (prevCgm.points.Count > 0)
                    {
                        bool bb = ((Math.Abs((int)prevCgm.points.First().X - (int)cgmElement.points[0].X) <= 1)
                                             &&
                                        (Math.Abs((int)prevCgm.points.First().Y - (int)cgmElement.points[0].Y) <= 1));

                        bool aa = ((Math.Abs((int)prevCgm.points.Last().X - (int)cgmElement.points[0].X) <= 1)
                                             &&
                                        (Math.Abs((int)prevCgm.points.Last().Y - (int)cgmElement.points[0].Y) <= 1));

                        connectedPt = aa ? prevCgm.points.Last() : prevCgm.points.First();

                    }
                    cgmElement.points.Add(new PointF(p_end.X, p_end.Y));

                    
                    string arcTO = string.Format(" M {0} {1} A {2} {3} {4} {5} {6} {7} {8} ", p_start.X, p_start.Y, rx, ry, angle_s, path_len, dir, p_end.X, p_end.Y);
                    path.Attributes["d"].Value += arcTO;
                    if (cgmElement.cir_arc_closure != null)
                    {
                        if (cgmElement.cir_arc_closure == "chord closure")
                        {
                            path.Attributes["d"].Value += string.Format("L {0} {1} Z ", p_start.X, p_start.Y);
                        }
                        else if (cgmElement.cir_arc_closure == "pieclosure")
                        {
                            path.Attributes["d"].Value += string.Format("L {0} {1} {2} {3} Z ", cgmElement.points[0].X, cgmElement.points[0].Y, p_start.X, p_start.Y);
                        }
                    }
                 

                    #endregion
                }
                else if (cgmElement.elem_Name == "PARABOLIC ARC")
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value = "";
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    }
                    StringBuilder arc = new StringBuilder();
                    arc.Append(string.Format("M {0} {1} ", cgmElement.points[1].X, cgmElement.points[1].Y));
                    arc.Append(string.Format("C {0} {1} ", cgmElement.points[3].X, cgmElement.points[3].Y));
                    arc.Append(string.Format("{0} {1} ", cgmElement.points[4].X, cgmElement.points[4].Y));
                    arc.Append(string.Format("{0} {1} ", cgmElement.points[2].X, cgmElement.points[2].Y));
                    path.Attributes.Append(cgm_svg.CreateAttribute("d")).Value += arc.ToString();
                    #endregion

                }
                else if (cgmElement.elem_Name == "TEXT")
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("text");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("text"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("transform"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("x"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("y"));
                    path.InnerText = cgmElement.text;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes["x"].Value = "0";
                    path.Attributes["y"].Value = "0";
                    path.Attributes["transform"].Value += String.Format("translate({0} {1})", cgmElement.position.X, (cgmElement.position.Y + (cgmElement.characterHeight * 0.79)));
                    path.Attributes.Append(cgm_svg.CreateAttribute("font-size")).Value = (cgmElement.characterHeight).ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("lengthAdjust")).Value = "spacingAndGlyphs";
                    path.Attributes.Append(cgm_svg.CreateAttribute("xml:space")).Value = "preserve";
                    path.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-family:{0};", cgmElement.fontfamily);

                    if (cgmElement.h_alignment_name == "center")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "middle";
                    }
                    else if (cgmElement.h_alignment_name == "right")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "end";
                    }
                    else if (cgmElement.h_alignment_name == "left")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "start";
                    }
                    else
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "start";
                    }


                    if (cgmElement.v_alignment_name == "top")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "top";
                    }
                    else if (cgmElement.v_alignment_name == "base")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "baseline ";
                    }
                    else if (cgmElement.v_alignment_name == "bottom")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "baseline ";
                    }
                    else if (cgmElement.v_alignment_name == "half")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "middle";
                    }
                    else
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "";
                    }

                    foreach (string[] txtAppend in cgmElement.appendedText)
                    {
                        XmlNode tspan = cgm_svg.CreateElement("tspan");
                        tspan.InnerText = txtAppend[1];
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-size:{0};", txtAppend[0]);
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("font-size")).Value = txtAppend[0];
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-family:{0};", cgmElement.fontfamily);
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("lengthAdjust")).Value = "spacingAndGlyphs";
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("xml:space")).Value = "preserve";

                        path.AppendChild(tspan);
                    }
                    cgm_svg.DocumentElement.AppendChild(path);
                    if (cgmElement.characterOrientation != null)
                    {
                        double upAngle = Math.Atan(cgmElement.characterOrientation[1] / cgmElement.characterOrientation[0]);
                        double baseAngle = Math.Atan(cgmElement.characterOrientation[3] / cgmElement.characterOrientation[2]);
                        upAngle = (upAngle * 180 / Math.PI);
                        baseAngle = (baseAngle * 180 / Math.PI);

                        double rotation = (int)(Math.Round(upAngle - baseAngle));
                        if (Math.Abs(rotation) == 90)
                        {
                            rotation = (90 - upAngle - baseAngle);
                            path.Attributes["transform"].Value += String.Format(" rotate({0})", -1 * baseAngle);
                        }
                        else
                        {
                            upAngle = upAngle == 90 ? 0 : upAngle;
                            baseAngle *= -1;
                            path.Attributes["transform"].Value += String.Format(" skewX({0}) skewY({1})", -1 * (90 - upAngle), baseAngle);
                        }
                    }
                    #endregion
                }
                else if (cgmElement.elem_Name == "RESTRICTED TEXT")
                {
                    #region MyRegion
                    path = cgm_svg.CreateElement("text");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("text"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("transform"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("x"));
                    path.Attributes.Append(cgm_svg.CreateAttribute("y"));
                    path.InnerText = cgmElement.text;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes["x"].Value = "0";
                    path.Attributes["y"].Value = "0";
                    path.Attributes["transform"].Value += String.Format("translate({0} {1})", cgmElement.position.X, (cgmElement.position.Y + (cgmElement.characterHeight *0.79 )).ToString());
                    foreach (string[] txtAppend in cgmElement.appendedText)
                    {
                        XmlNode tspan = cgm_svg.CreateElement("tspan");
                        tspan.InnerText = txtAppend[1];
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-size:{0};", txtAppend[0]);
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-family:{0};", cgmElement.fontfamily);
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("font-size")).Value = txtAppend[0];
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("lengthAdjust")).Value = "spacingAndGlyphs";
                        tspan.Attributes.Append(cgm_svg.CreateAttribute("xml:space")).Value = "preserve";
                        path.AppendChild(tspan);
                    }

                    path.Attributes.Append(cgm_svg.CreateAttribute("textLength")).Value = (cgmElement.delta_width).ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("font-size")).Value = (cgmElement.delta_height).ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("style")).Value += String.Format("font-family:{0};", cgmElement.fontfamily);
                    path.Attributes.Append(cgm_svg.CreateAttribute("lengthAdjust")).Value = "spacingAndGlyphs";
                    path.Attributes.Append(cgm_svg.CreateAttribute("xml:space")).Value = "preserve";
                    if (cgmElement.h_alignment_name == "center")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "middle";
                    }
                    else if (cgmElement.h_alignment_name == "right")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "end";
                    }
                    else if (cgmElement.h_alignment_name == "left")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "start";
                    }
                    else
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("text-anchor")).Value = "start";
                    }


                    if (cgmElement.v_alignment_name == "top")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "top";
                    }
                    else if (cgmElement.v_alignment_name == "base")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "baseline ";
                    }
                    else if (cgmElement.v_alignment_name == "bottom")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "baseline ";
                    }
                    else if (cgmElement.v_alignment_name == "half")
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "middle";
                    }
                    else
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("alignment-baseline")).Value = "";

                    }

                    cgm_svg.DocumentElement.AppendChild(path);
                    if (cgmElement.characterOrientation != null)
                    {
                        double upAngle = Math.Atan(cgmElement.characterOrientation[1] / cgmElement.characterOrientation[0]);
                        double baseAngle = Math.Atan(cgmElement.characterOrientation[3] / cgmElement.characterOrientation[2]);
                        upAngle = (upAngle * 180 / Math.PI);
                        baseAngle = (baseAngle * 180 / Math.PI);

                        double rotation = (int)(Math.Round(upAngle - baseAngle));
                        if (Math.Abs(rotation) == 90)
                        {
                            rotation = (90 - upAngle - baseAngle);
                            path.Attributes["transform"].Value += String.Format(" rotate({0})", -1 * baseAngle);
                        }
                        else
                        {
                            upAngle = upAngle == 90 ? 0 : upAngle;
                            baseAngle *= -1;
                            path.Attributes["transform"].Value += String.Format(" skewX({0}) skewY({1})", upAngle, baseAngle);
                        }
                    }

                    #endregion
                }
                else if (cgmElement.elem_Name == "NEW REGION")
                {
                    #region MyRegion
                    pathNew = false;
                    if (path.Attributes["d"] != null)
                    {
                        path.Attributes["d"].Value += " M";
                    }
                    #endregion
                }
                else
                {
                    path = cgm_svg.CreateElement("g");
                    pathNew = false;
                }
                #endregion
                if (pathNew)
                {
                    prevCgm = cgmElement;
                    cgm_idx++;
                    if (path.Attributes["style"] == null)
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("style"));
                    }
                    #region Apply Style

                    if (cgmElement.isFilledArea() || isFigure)
                    {
                        path.Attributes["style"].Value += string.Format("stroke:#{0};", cgmElement.edgeColor.Name.Substring(2));
                        path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");
                        if (cgmElement.elem_Name == "RESTRICTED TEXT")
                        {
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.characterColor.Name.Substring(2));
                        }
                        else if (cgmElement.elem_Name == "TEXT")
                        {
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.characterColor.Name.Substring(2));
                        }
                        else if (cgmElement.fill_style == "empty")
                        {
                            path.Attributes["style"].Value += string.Format("fill:none;");
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        else if (cgmElement.fill_style == "solid")
                        {
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.fillColor.Name.Substring(2));
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        else if (cgmElement.fill_style == "hollow")
                        {
                            path.Attributes["style"].Value += string.Format("fill:none;");
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", Math.Max(1, cgmElement.edgeWidth));
                        }
                        else if (cgmElement.fill_style == "hatch")
                        {
                            string colorName = cgmElement.fillColor.Name.Substring(2);
                            string patternID = cgmElement.hatch_id + "_" + colorName;
                            colorName = "#" + colorName;
                            createPatter(patternID, cgmElement.hatch_id, cgm_svg, colorName, 0.25f);
                            path.Attributes["style"].Value += string.Format("fill:url(#{0});", patternID);
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        else if (cgmElement.fill_style == "pattern")
                        {
                            path.Attributes["style"].Value += string.Format("fill:url(#img_patten_{0});", cgmElement.pattern_idx);
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        else
                        {
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        int edgeType;
                        if (int.TryParse(cgmElement.edgeType, out edgeType))
                        {
                            path.Attributes["style"].Value += string.Format("stroke-dasharray:{0};", cgmElement.getDashArray(lineEdgeTypeLookUp, edgeType));
                        }
                    }
                    else
                    {

                        if (cgmElement.elem_Name == "RESTRICTED TEXT")
                        {
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.characterColor.Name.Substring(2));
                            path.Attributes["style"].Value += string.Format("font-size:{0};", cgmElement.characterHeight);
                        }
                        else if (cgmElement.elem_Name == "TEXT")
                        {
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.characterColor.Name.Substring(2));
                            path.Attributes["style"].Value += string.Format("font-size:{0};", cgmElement.characterHeight);
                        }
                        else
                        {

                            if (isFigure)
                            {
                                if (cgmElement.fill_style == "solid")
                                {
                                    path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");
                                    path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.fillColor.Name.Substring(2));
                                }
                                else
                                {
                                    path.Attributes["style"].Value += string.Format("fill:none;");
                                }
                            }
                            else
                            {
                                path.Attributes["style"].Value += string.Format("fill:none;");
                            }

                            if (isFigure)
                            {
                                path.Attributes["style"].Value += string.Format("stroke:#{0};", cgmElement.edgeColor.Name.Substring(2));
                                if (cgmElement.edgeVisibility)
                                {
                                    path.Attributes["style"].Value += string.Format("stroke-width:{0};", Math.Max(0.01, cgmElement.edgeWidth));
                                }
                                else
                                {
                                    path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth);
                                }

                            }
                            else
                            {
                                path.Attributes["style"].Value += string.Format("stroke:#{0};", cgmElement.strokeColor.Name.Substring(2));
                                if (cgmElement.edgeVisibility)
                                {
                                    path.Attributes["style"].Value += string.Format("stroke-width:{0};", Math.Max(0.01, cgmElement.strokeWidth));
                                }
                                else
                                {
                                    path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.strokeWidth);
                                }

                            }
                            int edgeType;
                            if (int.TryParse(cgmElement.lineType, out edgeType))
                            {
                                path.Attributes["style"].Value += string.Format("stroke-dasharray:{0};", cgmElement.getDashArray(lineEdgeTypeLookUp, edgeType));
                            }
                        }

                    }


                    path.Attributes["style"].Value += string.Format("stroke-linecap:{0};", cgmElement.lineCap);
                    path.Attributes["style"].Value += string.Format("stroke-linejoin:{0};", cgmElement.lineJoin);
                    path.Attributes["style"].Value += string.Format("stroke-miterlimit:{0};", cgmElement.mitreLimit);

                    //path.Attributes["style"].Value += string.Format("stroke-dashoffset:{0};", "");                    
                    //path.Attributes["style"].Value += string.Format("marker-start:{0};", "");
                    //path.Attributes["style"].Value += string.Format("marker-mid:{0};", "");
                    //path.Attributes["style"].Value += string.Format("marker-end:{0};", ""); 
                    #endregion
                }
                cgm_idx_abs++;
            }
            if( pic_idx == lstSheet)
            {
                cgm_svg.Save(String.Format(@"C:\Users\795627\Desktop\cmg_svg_{0}_{1}.svg", vdcIdx.ToString(), pic_idx.ToString()));
            }
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        public class LineSegment
        {
            public PointF p1;
            public PointF p2;
            public LineSegment() { }
        }
        
        public class LineEdgeType
        {
            public int id;
            public float dashCycle_Length;
            public List<float> dashseq;
            public LineEdgeType()
            {
                id = 0;
                dashCycle_Length = 0;
                dashseq = new List<float>();
            }
        }

        public class Cgm_Element
        {
            #region Cgm_Element properties
            public Bitmap rasterImage;
            public float imageRotation;
            public float imageScale;
            public float imageScale_x;
            public float imageScale_y;
            public float imageScale_delta_y;
            public float imageScale_delta_x;
            public int vdc_idx;

            public PointF beginTilePoint;
            public int cellPathDirection;
            public int lineProgressionDirection;
            public int nTilesInPathDirection;
            public int nTilesInLineDirection;
            
            public int nCellsPerTileInPathDirection;
            public int nCellsPerTileInLineDirection;

            public float cellSizeInPathDirection;
            public float cellSizeInLineDirection;
           
            public int imageOffsetInPathDirection;
            public int imageOffsetInLineDirection;
           
            public int nCellsInPathDirection;
            public int nCellsInLineDirection;

            public string h_alignment_name;
            public string v_alignment_name;

            public int h_alignment;
            public int v_alignment;
            public float continuousVerticalAlignment;
            public float continousHorizontalAlignment;

            public int picture_idx;
            public float arc_angle;
            public bool isCircle;
            public float arc_radius;
            public bool isbottomUp;
            public bool isleftRight;
            public LineEdgeType lineEdgeDefs;
            public float scaleFactor;
            public bool isFig;
            public bool end_fig;
            public bool start_fig;
            public int colourTable_start_idx;
            public string cir_arc_closure;
            public string colorModel;
            public int maximum_colour_index;
            public int integer_precision;
            public int real_precision;
            public int text_precision;
            public int vdc_integer_precision;
            public int vdc_real_precision;
            public Color min_rgb;
            public Color max_rgb;
            public Color backgroundColor;
            public string colour_value_extent;
            public int colour_value_extent_size;
            public string elem_Class;
            public string elem_Id;
            public string vdcType;
            public string realType;
            public string vdc_realType;
            public string elem_Name;
            public string elem_NameAlt;
            public byte[] elemParams;
            public int param_length;
            public bool isFinalText;
            public bool long_form_list;
            public bool edgeVisibility;
            public List<PointF> points;
            public List<string> polygonSetFlags;
            public PointF position;
            public String text;
            public List<string[]> appendedText;
            public Color bgColor;
            public Color strokeColor;
            public Color fillColor;
            public Color edgeColor;
            public Color characterColor;
            public bool lineWidthSet;
            public bool edgeWidthSet;
            public float strokeWidth;
            public float edgeWidth;
            public string edgeType;
            public string lineType;
            public string scalingMode;
            public float characterHeight;
            public float width;
            public float height;
            public string fontlist;
            public string fontfamily;
            public List<string> fontlist_LIST;
            public float[] characterOrientation;
            public int polybezier_continuous;
            public float delta_width;
            public float delta_height;
            public PointF[] vdcExtent;
            public float page_height;
            public float page_width;
            public float true_height;
            public float true_width;
            public string fill_style;
            public string hatch_style;
            public string color_model;
            public string hatch_id;
            public string color_model_idx;
            public string pattern_idx;
            public string lineSizeMode;
            public string edgeSizeMode;
            public string markerSizeMode;
            public string lineJoin;
            public string lineCap;
            public string lineTypeContinue;
            public string dashCapIndicator;
            
            public string edgeJoin;
            public string edgeCap;
            public string edgeTypeContinue;
            public string edgedashCapIndicator;
            
            public string metafile_elements;
            public float mitreLimit;
            public string colourSelectionMode;
            public string character_set_list;
            public int colour_precision;
            public int pixel_precision;
            public int colour_idx_precision;
            public int idx_precision;
            public List<Color> colorTable;
            public List<Color> palette = new List<Color>();
            #endregion
            #region Cgm_Element Methods

            public Cgm_Element()
            {

                imageScale_x = imageScale_y = 1;
                palette = new List<Color>();
                palette.Add(Color.FromArgb(255, 0, 0, 0));
                palette.Add(Color.FromArgb(255, 128, 0, 0));
                palette.Add(Color.FromArgb(255, 255, 0, 0));
                palette.Add(Color.FromArgb(255, 255, 192, 203));
                palette.Add(Color.FromArgb(255, 0, 128, 128));
                palette.Add(Color.FromArgb(255, 0, 128, 0));
                palette.Add(Color.FromArgb(255, 0, 255, 0));

                palette.Add(Color.FromArgb(255, 64, 224, 208));
                palette.Add(Color.FromArgb(255, 0, 0, 139));
                palette.Add(Color.FromArgb(255, 238, 130, 238));
                palette.Add(Color.FromArgb(255, 0, 0, 255));
                palette.Add(Color.FromArgb(255, 192, 192, 192));
                palette.Add(Color.FromArgb(255, 128, 128, 128));
                palette.Add(Color.FromArgb(255, 128, 128, 0));
                palette.Add(Color.FromArgb(255, 255, 255, 0));
                palette.Add(Color.FromArgb(255, 255, 255, 255));


                true_height = page_height =
                true_width =  page_width = 32767f;

                imageScale_delta_y = 0;
                imageScale_delta_x = 0;
                imageRotation = 0;
                imageScale = 1;
                appendedText = new List<string[]>();
                colorTable = new List<Color>();
                colourTable_start_idx = 0;
                hatch_id = "1";
                pattern_idx = "1";
                isCircle = false;
                mitreLimit = 4;
                lineEdgeDefs = new LineEdgeType();
                end_fig = start_fig = false;
                vdcType = "integer";
                realType = "floating";
                vdc_realType = "floating";
                lineSizeMode = "scaled";
                edgeSizeMode = "scaled";
                markerSizeMode = "scaled";
                isFig = false;
                colorModel = "RGB";
                integer_precision = 16;
                vdc_real_precision = 16;
                colour_precision = 24;
                pixel_precision = 24;
                colour_idx_precision = 8;
                idx_precision = 16;
                real_precision = 32;
                vdc_integer_precision = 16;
                isleftRight = true;                
                elem_Class = "";
                elem_Id = "";
                elem_Name = "";
                page_height = 0;
                page_width = 0;
                elemParams = new byte[0];
                points = new List<PointF>();
                polygonSetFlags = new List<string>();

                characterColor = fillColor = strokeColor = edgeColor = Color.FromArgb(255, 0, 0, 0);
                lineWidthSet = edgeWidthSet = false;
                characterHeight = 16;
                edgeWidth = strokeWidth = 1f;
                lineCap = "round";
                lineJoin = "round";
                mitreLimit = 1f;
                edgeVisibility = false;
                fill_style = "hollow";
                lineType = "1";
                vdc_idx = 0;
                picture_idx = 0;

            }
            public void resetColors_fromColorTable()
            {
                fillColor = strokeColor = edgeColor = colorTable[0];
            }
            public int getPrecision_vdc()
            {
                return vdcType == "real" ? vdc_real_precision : vdc_integer_precision;
            }

            public string getDashArray(List<LineEdgeType> lineEdgeTypeLookUp, int id)
            {
                try
                {
                    float[] dashArray = lineEdgeTypeLookUp.Where(fd => fd.id == id).ToArray()[0].dashseq.Cast<float>().ToArray();

                    return String.Join(",", dashArray.Select(fd => fd.ToString()).ToArray());
                }
                catch (Exception ee)
                {

                    return "";
                }
            }

            public int getPrecision()
            {
                return vdcType == "real" ? real_precision : vdc_integer_precision;
            }

            public int getPrecision_int()
            {
                return  vdc_integer_precision;
            }

            public float bytes_getValue(byte[] bytes, int precision)
            {

                float result = 0;
                bool nop = false;
                if (vdcType == "integer")
                {
                    precision = vdc_integer_precision;
                    switch (precision)
                    {
                        case 8:
                            result = (byte)BitConverter.ToChar(bytes.Reverse().ToArray(), 0);
                            break;
                        case 16:

                            result = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
                            break;
                        case 32:
                            result = BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
                            break;
                        default:
                            nop = true;
                            break;
                    }
                    if (!nop)
                    {
                        return result;
                    }

                }
                else
                {
                    precision = vdc_real_precision;

                    if (vdc_realType == "floating")
                    {
                        if (precision == 32)
                        {
                            result = BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
                            return result;
                        }
                        else if (precision == 16)
                        {
                            double dd = 0;
                            if (bytes.Length == 4)
                            {
                                byte[] fraction = new byte[2];
                                Array.Copy(bytes, 2, fraction, 0, 2);
                                Array.Resize(ref bytes, 2);
                                dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) +(BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                            }
                            else
                            {
                                Array.Resize(ref bytes, 2);
                                dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
                            }
                            return (float)dd;

                        }
                    }
                    else if (vdc_realType == "fixed")
                    {
                        if (precision == 32)
                        {
                            byte[] fraction = new byte[2];
                            Array.Copy(bytes, 2, fraction, 0, 2);
                            Array.Resize(ref bytes, 2);
                            double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                            return (float)dd;
                        }
                    }
                }


                UInt32 signMask = (0xffffffff << precision);

                string binary = bytes.Select(a => Convert.ToString(a, 2).PadLeft(8, '0')).Aggregate((a, b) => a + b);
                UInt32 result_u = Convert.ToUInt32(binary, 2);
                if (binary[0] == '1')
                {
                    result = (int)(result_u | signMask);
                }
                else
                {
                    result = (int)result_u;
                }

                return result;
            }


            public float bytes_getValue_color(byte[] bytes, int precision)
            {

                float result = 0;
                bool nop = false;


                switch (precision)
                {
                    case 8:
                        result = bytes[0];
                        break;
                    case 16:
                        result = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
                        break;
                    case 32:
                        result = BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
                        break;
                    default:
                        nop = true;
                        break;
                }
                if (!nop)
                {
                    return result;
                }
                if (bytes.Length == 0)
                {
                    return result;
                }


                UInt32 signMask = (0xffffffff << precision);

                string binary = bytes.Select(a => Convert.ToString(a, 2).PadLeft(8, '0')).Aggregate((a, b) => a + b);
                UInt32 result_u = Convert.ToUInt32(binary, 2);
                if (binary[0] == '1')
                {
                    result = (int)(result_u | signMask);
                }
                else
                {
                    result = (int)result_u;
                }

                return result;
            }

            public float bytes_getValue_real(byte[] bytes, int precision)
            {
                
                float result = 0;
                precision = real_precision;
                if (realType == "floating")
                {

                    if (precision == 32)
                    {
                        result = BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
                        return result;
                    }
                    else if (precision == 16)
                    {
                        result = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
                        return result;
                    }
                }
                else
                {
                    if (precision == 32)
                    {
                        byte[] fraction = new byte[2];
                        Array.Copy(bytes, 2, fraction, 0, 2);
                        Array.Resize(ref bytes, 2);
                        double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                        return (float)dd;
                    }
                    if (precision == 64)
                    {

                    }
                }


                return result;
            }

            public float bytes_getValue_edge(byte[] bytes)
            {
                float result = 0;
                if (vdcType == "real")
                {
                    if (vdc_real_precision == 32 && vdc_realType == "floating")
                    {
                        result = BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);
                    }
                    if (vdc_real_precision == 32 && vdc_realType == "fixed")
                    {
                        byte[] fraction = new byte[2];
                        Array.Copy(bytes, 2, fraction, 0, 2);
                        Array.Resize(ref bytes, 2);
                        double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                        return (float)dd;
                    }
                    else if (vdc_real_precision == 16 && vdc_realType == "floating")
                    {
                        byte[] fraction = new byte[2];
                        Array.Copy(bytes, 2, fraction, 0, 2);
                        Array.Resize(ref bytes, 2);
                        double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                        return (float)dd;
                    }
                }
                else if (vdc_integer_precision == 32)
                {
                    result = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
                }
                else if (vdc_integer_precision == 16)
                {

                    Array.Resize(ref bytes, 2);
                    double dd = (BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0));
                    result =  (float)dd;                    
                }

                return result;
            }

            public float bytes_getValue_int(byte[] bytes, int precision)
            {
                float result = 0;
                switch (precision)
                {
                    case 8:
                        result = bytes[0];
                        break;
                    case 16:
                        result = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0);
                        break;
                    case 32:
                        result = BitConverter.ToInt32(bytes.Reverse().ToArray(), 0);
                        break;
                    default:
                        break;
                }
                return result;
            }
            public UInt32 bytes_getValue_uint(byte[] bytes, int precision)
            {
                UInt32 result = 0;
                switch (precision)
                {
                    case 8:
                        result = (byte)bytes[0];
                        break;
                    case 16:
                        result = BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0);
                        break;
                    case 32:
                        result = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
                        break;
                    default:
                        break;
                }
                return result;
            }
            public UInt32 make16_masked(byte[] bytes)
            {
                UInt32 result = BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0);
                return result & 0x7fff;
            }
            public UInt32 make16(byte[] bytes)
            {
                UInt32 result = BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0);
                return result;
            }
            public Color pixelColor(byte[] buffer, ref Color elemColor, int pixelSize)
            {
                if (pixelSize >= 3)
                {

                    int red = buffer[0];
                    int green = buffer[1];
                    int blue = buffer[2];
                    int alpha = 255;

                    if (pixelSize == 4)
                    {
                        alpha = (int)buffer[3];
                    }
                    elemColor = Color.FromArgb(alpha, red, green, blue);
                }
                if (pixelSize == 1)
                {
                    if (colorTable == null)
                    {
                        int r = (buffer[0] >> 5);
                        int g = ((buffer[0] >> 2) & 0x7);
                        int b = (buffer[0] & 0x3);

                        r = r * 255 / 7;
                        g = g * 255 / 7;
                        b = b * 255 / 3;

                        elemColor = Color.FromArgb(255, r, g, b);

                    }
                    else if (colorTable.Count() > buffer[0])
                    {
                        elemColor = colorTable[buffer[0]];
                    }
                }
                else if (pixelSize < 3)
                {
                    colour_precision = 8 * pixelSize;
                    int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                    if (colorTable == null)
                    {
                        if (colour_precision == 8)
                        {
                            int r = (buffer[0] >> 5);
                            int g = ((buffer[0] >> 2) & 0x7);
                            int b = (buffer[0] & 0x3);
                            r = r * 255 / 7;
                            g = g * 255 / 7;
                            b = b * 255 / 3;
                            elemColor = Color.FromArgb(255, r, g, b);
                        }
                        else if (colour_precision == 16)
                        {
                            int r = (colr_idx >> 11);
                            int g = (colr_idx >> 5) & 0x3f;
                            int b = (colr_idx & 0x1f);
                            r = r * 255 / 31;
                            g = g * 255 / 63;
                            b = b * 255 / 31;
                            elemColor = Color.FromArgb(255, r, g, b);
                        }
                        else
                        {
                            elemColor = Color.FromArgb(255, colr_idx, colr_idx, colr_idx);
                        }
                    }
                    else if (colorTable.Count() > colr_idx)
                    {
                        elemColor = colorTable[colr_idx];
                    }
                }

                return elemColor;
            }

            public Color extractColor(byte[] buffer, ref Color elemColor)
            {
                if (buffer.Length >= 3)
                {

                    int red = buffer[0];
                    int green = buffer[1];
                    int blue = buffer[2];
                    int alpha = 255;

                    if (buffer.Length == 4)
                    {
                        alpha = (int)buffer[3];
                    }
                    if (buffer.Length == 6)
                    {
                        red = buffer[1];
                        green = buffer[3];
                        blue = buffer[5];
                    }
                    
                    elemColor = Color.FromArgb(alpha, red, green, blue);
                }
                if (buffer.Length == 1)
                {
                    if (colorTable.Count() == 0)
                    {
                        if(palette.Count() > buffer[0])
                        {
                            elemColor = palette[buffer[0]];
                        }
                        else
                        {
                            elemColor = buffer[0] > 0 ? Color.FromArgb(255, buffer[0], buffer[0], buffer[0]) : Color.FromArgb(255, 255, 255, 255);
                        }
                    }                
                    else if (colorTable.Count() > buffer[0])
                    {
                        elemColor = colorTable[buffer[0]];
                    }

                }
                else if (buffer.Length < 3)
                {
                    colour_precision = 8 * param_length;
                    int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                    if (colorTable == null)
                    {
                        elemColor = Color.FromArgb(255, colr_idx, colr_idx, colr_idx);
                    }
                    else if (colorTable.Count() > colr_idx)
                    {
                        elemColor = colorTable[colr_idx];
                    }
                }
                return elemColor;
            }

            public Color extractColor(byte[] buffer, bool directColor)
            {
                Color elemColor = new Color();
                if (directColor)
                {
                    if (param_length >= 3)
                    {

                        int red = buffer[0];
                        int green = buffer[1];
                        int blue = buffer[2];
                        int alpha = 255;

                        if (param_length == 4)
                        {
                            alpha = (int)buffer[3];
                        }
                        elemColor = Color.FromArgb(alpha, red, green, blue);
                    }
                    if (param_length == 1)
                    {
                        int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                        elemColor = Color.FromArgb(255, colr_idx, colr_idx, colr_idx);
                    }
                }
                else if (buffer.Length == 1)
                {
                    int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                    elemColor = Color.FromArgb(255, colr_idx, colr_idx, colr_idx);
                }
                else 
                {
                    colour_precision = 8 * buffer.Length;
                    int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                    if (colorTable == null)
                    {
                        
                    }
                    else if (colorTable.Count() > colr_idx)
                    {
                        elemColor = colorTable[colr_idx];
                    }
                }

                return elemColor;
            }
            public Color getColor(int color_precision, BinaryReader br)
            {
                Color elemColor = new Color();
                int bytesRead = 0;
                byte[] buffer;
                int colorBytes = color_precision / 8;
                if (colorBytes == 4)
                {
                    buffer = new byte[colorBytes];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    elemColor = Color.FromArgb(buffer[0], (int)buffer[1], (int)buffer[2], (int)buffer[3]);

                }
                else if (colorBytes == 3)
                {
                    buffer = new byte[colorBytes];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    elemColor = Color.FromArgb(255, (int)buffer[0], (int)buffer[1], (int)buffer[2]);

                }
                else if (colorBytes == 1)
                {
                    buffer = new byte[colorBytes];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    int cval = buffer[0];
                    string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');
                    binStr = binStr.PadLeft(8, '0');
                    buffer = new byte[4];
                    if (colourSelectionMode == "indexed colour mode")
                    {
                        buffer = new byte[4];
                        buffer[0] = 
                        buffer[1] =
                        buffer[2] = (byte)(cval * 255 / maximum_colour_index);
                    }
                    else
                    {
                        buffer = new byte[4];
                        for (int j = 0; j < 4; j++)
                        {
                            string c = binStr.ToCharArray()[j * 2].ToString() + binStr.ToCharArray()[(j * 2) + 1].ToString();
                            buffer[j] = (byte)(Convert.ToUInt16(c, 10) );
                        }
                        
                    }
                    elemColor = Color.FromArgb(255, (int)buffer[0], (int)buffer[1], (int)buffer[2]);

                }
                else if (color_precision == 1)
                {
                    buffer = new byte[1];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    int cval = (int)bytes_getValue_int(buffer, 8);
                    string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');
                    binStr = binStr.PadLeft(8, '0');
                    foreach (char c in binStr.ToCharArray())
                    {
                        elemColor = c == '0' ? Color.Black : Color.White;                        
                    }
                }
                else if (color_precision == 2)
                {
                    buffer = new byte[1];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    int cval = (int)bytes_getValue_int(buffer, 8);
                    string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');
                    for (int j = 0; j < 4; j++)
                    {
                        string c = binStr.ToCharArray()[j * 2].ToString() + binStr.ToCharArray()[(j * 2) + 1].ToString();
                        int intensity = (byte)Convert.ToUInt16(c, 2);
                        intensity = intensity * 255 / 7;
                        elemColor = Color.FromArgb(255, intensity, intensity, intensity);

                    }

                }
                else if (color_precision == 4)
                {
                    buffer = new byte[1];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    int cval = (int)bytes_getValue_int(buffer, 8);
                    string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');



                    for (int j = 0; j < 2; j++)
                    {
                        string c =
                            binStr.ToCharArray()[(j * 4) + 0].ToString() +
                            binStr.ToCharArray()[(j * 4) + 1].ToString() +
                            binStr.ToCharArray()[(j * 4) + 2].ToString() +
                            binStr.ToCharArray()[(j * 4) + 3].ToString();
                        int intensity = (byte)Convert.ToUInt16(c, 2);
                        elemColor = colorTable[intensity];

                    }
                }
                else if (color_precision == 0)
                {
                    buffer = new byte[colour_idx_precision / 8];
                    int idx = (int)bytes_getValue_int(buffer, (int)color_precision);
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    elemColor = new Color();
                    if (idx < colorTable.Count)
                    {
                        elemColor = colorTable[idx];
                    }

                }
                return elemColor;
            }
            public bool isFilledArea()
            {
                System.Collections.Hashtable fillarea = new System.Collections.Hashtable();
                fillarea.Add("POLYGON", true);
                fillarea.Add("CIRCULAR ARC 3 POINT CLOSE", true);
                fillarea.Add("POLYGON SET", true);
                fillarea.Add("CIRCULAR ARC CENTRE CLOSE", true);
                fillarea.Add("RECTANGLE", true);
                fillarea.Add("ELLIPSE", true);
                fillarea.Add("CIRCLE", true);
                fillarea.Add("ELLIPTICAL ARC CLOSE", true);


                return fillarea[elem_Name] == null ? false : (bool)fillarea[elem_Name];
            }

            public bool isContinuous()
            {
                if (polybezier_continuous == 1)
                {
                    return false;
                }
                else if (polybezier_continuous == 2)
                {
                    return true;
                }
                return false;
            }

            public string raster2base64()
            {
                string base64String = "";

                if (rasterImage != null)
                {
                    MemoryStream m = new MemoryStream();
                    rasterImage.Save(m, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] imageBytes = m.ToArray();
                    base64String = Convert.ToBase64String(imageBytes, Base64FormattingOptions.None);
                }
                return base64String;
            }

            #endregion
        }

        public List<LineEdgeType> lineEdgeTypeLookUp = new List<LineEdgeType>();

        public void parseMetaElement(ref BinaryReader br, ref byte[] buf, ref int bytesread, ref int paramLen, ref byte elemclass, ref byte elemId, ref string elemName, ref List<Cgm_Element> Cgm_Elements, bool getNext)
        {
            byte[] buffer = buf;
            string str = "";
            if (debug == true)
            {
                Console.WriteLine(elemName.ToString());
            }
         
            if (paramLen >= 31)
            {
                getNextMetaLongLenth(br, buffer, out  paramLen);
            }
            Cgm_Elements.Last().param_length = paramLen;


            if (elemName == "CELL ARRAY")
            {
                #region CELL ARRAY
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = 3;
                int bytesRead = 0;
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));
                    words_cnt -= 1;
                }
                PointF[] pqr = Cgm_Elements.Last().points.ToArray();

                
                double angle;
                double angle2;
                float slope;
                float b_int;
                
                double PR = distance_180_xrs(pqr[0], pqr[2], out angle, out angle2, out slope, out b_int);
                
                float sy = (float)(PR * Math.Sin(angle * Math.PI / 180));
                float sx = (float)(PR * Math.Cos(angle * Math.PI / 180));
                PointF pointS = new PointF( pqr[1].X - sx, pqr[1].Y - sy);

                Cgm_Elements.Last().points.Add(pointS);
                pqr = Cgm_Elements.Last().points.ToArray();


                double PQ = distance_180_xrs(pqr[0], pqr[1], out angle, out angle2, out slope, out b_int);                
                double QR = distance_180_xrs(pqr[0], pqr[2], out angle, out angle2, out slope, out b_int);
                

                List<float> xVals = pqr.Select(fd => fd.X).ToList();
                List<float> yVals = pqr.Select(fd => fd.Y).ToList();
                xVals.Sort();
                yVals.Sort();

                float w_rt = (float)(Math.Abs( xVals.First() - xVals.Last()));
                float h_rt = (float)(Math.Abs( yVals.First() - yVals.Last()));
                
                float w = (float)PR;
                float h = (float)QR;

                if (w < 1 && h < 1)
                {
                    w *= 1000;
                    h *= 1000;

                    w_rt *= 1000;
                    h_rt *= 1000;

                    
                    Cgm_Elements.Last().imageScale = 1f / 1000;
                    
                }
                

                angle = -1 * Math.Round(angle, 0);

                precision = Cgm_Elements.Last().getPrecision_int();
                byteLen = precision / 8;         
                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int nx = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, precision);
         

                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int ny = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, precision);

                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int color_precision = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, precision);

                int bcnt = Cgm_Elements.Last().idx_precision/8;
                buffer = new byte[2];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                float row_mode = Cgm_Elements.Last().bytes_getValue_int(buffer, 16);

                bool indexedColor = Cgm_Elements.Last().colorTable.Count() > 0;
                List<Color> pixels = new List<Color>();


                
                #endregion

                if (row_mode == 0)
                {
                    #region MyRegion
                    int nColor = (int)(nx * ny);
                    bytesRead = 0;
                    int i = 0;
                    int numColors = 0;
                    while (pixels.Count() < nColor)
                    {

                        buffer = new byte[2];
                        br.Read(buffer, 0, buffer.Length);
                        buffer[0] = (byte)(buffer[0] & 0x7f);
                        int bytesPerRow = (int)Cgm_Elements.Last().bytes_getValue(buffer, 16);
                        bytesRead = 0;

                        while (bytesRead < bytesPerRow)
                        {
                            buffer = new byte[2];
                            bytesRead += br.Read(buffer, 0, buffer.Length);

                            numColors = (int)Cgm_Elements.Last().bytes_getValue(buffer, 16);
                            i += numColors;

                            Color elemColor = new Color();
                            if (indexedColor)
                            {
                                buffer = new byte[1];
                            }
                            else
                            {
                                buffer = new byte[3];
                            }
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            Cgm_Elements.Last().pixelColor(buffer, ref elemColor, buffer.Length);


                            while (numColors > 0)
                            {
                                pixels.Add(elemColor);
                                numColors -= 1;
                            }
                            if (i % nx == 0 && (bytesPerRow - bytesRead) > 0)
                            {
                                buffer = new byte[bytesPerRow - bytesRead];
                                bytesRead += br.Read(buffer, 0, buffer.Length);
                            }
                        }

                    }
                    #endregion
                }
                else if (row_mode == 1)
                {
                    #region MyRegion
                    int len = paramLen - bytesRead;
                    
                    
                    for (int i = 0; i < len; i++)
                    {
                        Color elemColor = new Color();
                        int colorBytes = color_precision / 8;                                                
                        if(colorBytes == 4)
                        {
                            buffer = new byte[colorBytes];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                             elemColor = Color.FromArgb(buffer[0], (int)buffer[1], (int)buffer[2], (int)buffer[3]);
                             pixels.Add(elemColor);
                        }
                        else if (colorBytes == 3) 
                        {
                            buffer = new byte[colorBytes];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                             elemColor = Color.FromArgb(255, (int)buffer[0], (int)buffer[1], (int)buffer[2]);
                             pixels.Add(elemColor);
                        }
                        else if (colorBytes == 1)
                        {
                            buffer = new byte[colorBytes];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            int cval = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, (int)color_precision);
                            string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');
                            binStr = binStr.PadLeft(8, '0');
                            buffer = new byte[4];
                            for (int j = 0; j < 4; j++)
                            {
                                string c = binStr.ToCharArray()[j * 2].ToString() + binStr.ToCharArray()[(j * 2) + 1].ToString();
                                 buffer[j] = (byte)Convert.ToUInt16(c, 10);
                            }
                            elemColor = Color.FromArgb(255, (int)buffer[0], (int)buffer[1], (int)buffer[2]);
                            pixels.Add(elemColor);
                        }                        
                        else if (color_precision == 1) 
                        {
                            buffer = new byte[1];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            int cval = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, 8);
                            string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');
                            binStr = binStr.PadLeft(8, '0');
                            foreach (char c in binStr.ToCharArray())
                            {
                                elemColor = c == '0' ? Color.Black : Color.White;
                                pixels.Add(elemColor);
                            }                            
                        }                        
                        else if (color_precision == 2) 
                        {
                            buffer = new byte[1];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            int cval = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, 8);
                            string binStr = Convert.ToString(cval, 2).PadLeft(8,'0');
                            for (int j = 0; j < 4; j++)
                            {
                                string c = binStr.ToCharArray()[j * 2].ToString() + binStr.ToCharArray()[(j * 2) + 1].ToString();
                                int intensity = (byte)Convert.ToUInt16(c, 2);
                                intensity = intensity  * 255 / 7;
                                elemColor = Color.FromArgb(255, intensity, intensity, intensity);
                                pixels.Add(elemColor);
                            }

                        }
                        else if (color_precision == 4) 
                        {
                            buffer = new byte[1];
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            int cval = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, 8);
                            string binStr = Convert.ToString(cval, 2).PadLeft(8, '0');

                            

                            for (int j = 0; j < 2; j++)
                            {
                                string c =
                                    binStr.ToCharArray()[(j * 4) + 0].ToString() +
                                    binStr.ToCharArray()[(j * 4) + 1].ToString() +
                                    binStr.ToCharArray()[(j * 4) + 2].ToString() +
                                    binStr.ToCharArray()[(j * 4) + 3].ToString();
                                int intensity = (byte)Convert.ToUInt16(c, 2);                                
                                elemColor = Cgm_Elements.Last().colorTable[intensity];
                                pixels.Add(elemColor);
                            }
                        }
                        else if (color_precision == 0)
                        {
                            buffer = new byte[Cgm_Elements.Last().colour_idx_precision / 8];
                            int idx = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, (int)color_precision);
                            bytesRead += br.Read(buffer, 0, buffer.Length);
                            elemColor = new Color();
                            if (idx < Cgm_Elements.Last().colorTable.Count)
                            {
                                elemColor = Cgm_Elements.Last().colorTable[idx];
                            }
                            pixels.Add(elemColor);
                        }
                        

                        
                        
                        
                    }

                    #endregion
                }

                #region Create Bitmap
                Cgm_Elements.Last().rasterImage = new Bitmap((int)w_rt, (int)h_rt);
                Cgm_Elements.Last().imageRotation = 0;
                
                Graphics g = Graphics.FromImage(Cgm_Elements.Last().rasterImage);
                if (angle != 0)
                {
                    g.TranslateTransform(0, h_rt / 2);
                    g.RotateTransform((float)angle);
                }
                

                float xrex =(float)Math.Round(w / nx, 0);

                float yres = (float)Math.Round(h / ny, 0);

                int k = 0;
                
                for (int i = 0; i < ny; i ++)
                {
                    for (int j = 0; j < nx; j ++)
                    {
                        Color pixel_c = pixels[k];
                        g.FillRectangle(new SolidBrush(pixel_c), j * xrex, i * yres, (int)xrex, (int)yres);
                        k++;
                    }
                }
                g.TranslateTransform(-w / 2, -h / 2); 
                
                #endregion
                if (br.BaseStream.Position % 2 != 0)
                {
                    bytesRead += br.Read(buffer, 0, 1);
                }  
                #endregion                
            }
            else if (elemName == "TEXT ALIGNMENT")
            {
                #region MyRegion
                int real_p = Cgm_Elements.Last().real_precision;
                int len_first2parameters = paramLen - (real_p / 4);
                int bytcnt = len_first2parameters / 2;
                buffer = new byte[bytcnt];
                br.Read(buffer, 0, buffer.Length);
                int h_alignment = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                string alignment = "";
                switch (h_alignment)
                {
                    case 0:
                        alignment = "normal horizontal";
                        break;
                    case 1:
                        alignment = "left";
                        break;
                    case 2:
                        alignment = "center";
                        break;
                    case 3:
                        alignment = "right";
                        break;
                    case 4:
                        alignment = "continuous horizontal";
                        break;
                }
                Cgm_Elements.Last().h_alignment = h_alignment;
                Cgm_Elements.Last().h_alignment_name = alignment;


                buffer = new byte[bytcnt];
                br.Read(buffer, 0, buffer.Length);
                int v_alignment = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);
                switch (h_alignment)
                {
                    case 0:
                        alignment = "normal vertical";
                        break;
                    case 1:
                        alignment = "top";
                        break;
                    case 2:
                        alignment = "cap";
                        break;
                    case 3:
                        alignment = "half";
                        break;
                    case 4:
                        alignment = "base";
                        break;
                    case 5:
                        alignment = "bottom";
                        break;
                    case 6:
                        alignment = "continuous vertical";
                        break;
                }
                Cgm_Elements.Last().v_alignment = v_alignment;
                Cgm_Elements.Last().v_alignment_name = alignment;


                bytcnt = real_p / 8;
                buffer = new byte[bytcnt];
                br.Read(buffer, 0, buffer.Length);
                float continousHorizontalAlignment = Cgm_Elements.Last().bytes_getValue_real(buffer, real_p);
                Cgm_Elements.Last().continousHorizontalAlignment = continousHorizontalAlignment;


                buffer = new byte[bytcnt];
                br.Read(buffer, 0, buffer.Length);
                float continuousVerticalAlignment = Cgm_Elements.Last().bytes_getValue_real(buffer, real_p);

                Cgm_Elements.Last().continuousVerticalAlignment = continuousVerticalAlignment; 
                #endregion
            }
            else if (elemName == "BEGIN TILE ARRAY")
            {
                #region BEGIN TILE ARRAY
                int bytesRead = 0;

                int p = Cgm_Elements.Last().getPrecision();
                int b_len = p / 8;
                int words_cnt = 1;
                buffer = new byte[paramLen];

                while (words_cnt > 0)
                {
                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().beginTilePoint = new PointF() { X = px, Y = py };
                    words_cnt = words_cnt - 1;
                }


                buffer = new byte[2];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().cellPathDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, 16);


                buffer = new byte[2];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().lineProgressionDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, 16);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nTilesInPathDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nTilesInLineDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);


                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nCellsPerTileInPathDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nCellsPerTileInLineDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);



                buffer = new byte[Cgm_Elements.Last().real_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().cellSizeInPathDirection = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().real_precision);

                buffer = new byte[Cgm_Elements.Last().real_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().cellSizeInLineDirection = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().real_precision);



                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int imageOffsetInPathDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().imageOffsetInLineDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);


                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nCellsInPathDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().nCellsInLineDirection = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision); 
                #endregion
            }
            else if (elemName == "BITONAL TILE")
            {
                #region MyRegion
                #region MyRegion
                Cgm_Element beginTileArray = Cgm_Elements.Last(fd => fd.elem_Name == "BEGIN TILE ARRAY");
                Cgm_Elements.Last().beginTilePoint = beginTileArray.beginTilePoint;
                Cgm_Elements.Last().cellSizeInPathDirection = beginTileArray.cellSizeInPathDirection;
                Cgm_Elements.Last().cellSizeInLineDirection = beginTileArray.cellSizeInLineDirection;
                int bytesRead = 0;
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int compression_type = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                bcnt = Cgm_Elements.Last().integer_precision / 8;
                buffer = new byte[bcnt];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int row_padding = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().integer_precision);

                int byteLen = Cgm_Elements.Last().colour_precision / 8;

                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Color backGroundColor = new Color();
                Cgm_Elements.Last().extractColor(buffer, ref backGroundColor);

                buffer = new byte[byteLen];
                Color foreGroundColor = new Color();
                bytesRead += br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref foreGroundColor);


                #endregion

                if (compression_type < 6)
                {
                    #region Create Bitmap
                    int w = beginTileArray.nCellsPerTileInPathDirection;
                    int h = beginTileArray.nCellsPerTileInLineDirection;
                    Cgm_Elements.Last().imageScale_x = 1 / beginTileArray.cellSizeInPathDirection;
                    Cgm_Elements.Last().imageScale_y = 1 / beginTileArray.cellSizeInLineDirection;
                    

                    int x, y;
                    x = y = 0;

                    int remainingBytes = paramLen - bytesRead;
                    byte[] compressedData = new byte[remainingBytes];                    
                    bytesRead = br.Read(compressedData, 0, compressedData.Length);
                    y = y + bytesRead;

                    int len = paramLen;

                    while (len >= 20000)
                    {
                        int idx = compressedData.Length;
                        buffer = new byte[2];
                        bytesRead = br.Read(buffer, 0, buffer.Length);
                        len = (int)Cgm_Elements.Last().make16(buffer) & 0x7fff;
                        Array.Resize(ref compressedData, compressedData.Length + (int)len);
                        bytesRead = br.Read(compressedData, idx, len);
                        y = y + bytesRead;


                    }
                    
                    #endregion

                    len = compressedData.Length;
                    while (len % 2 != 0)
                    {
                        len--;
                    }
                    while (len != compressedData.Length)
                    {
                        byte[] a = new byte[len];
                        if (compressedData.First() == 0)
                        {
                            Array.Copy(compressedData, 1, a, 0, len);
                            compressedData = a;
                        }
                        else
                        {
                            Array.Resize(ref compressedData, len);
                        }
                    }
                    #region MyRegion
                    string filename = DateTime.Now.Ticks.ToString();

                    MemoryStream ms = new MemoryStream();
                    TiffStream ts = new TiffStream();
                    //using (Tiff output = Tiff.Open(filename+".tif", "w"))
                    using (Tiff output = Tiff.ClientOpen("in-memory", "w", ms, ts))
                    {

                        output.SetField(TiffTag.IMAGEWIDTH, w);
                        output.SetField(TiffTag.IMAGELENGTH, h);
                        output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                        output.SetField(TiffTag.BITSPERSAMPLE, 1);
                        output.SetField(TiffTag.ROWSPERSTRIP, h);
                        output.SetField(TiffTag.STRIPBYTECOUNTS, compressedData.Length);
                        output.SetField(TiffTag.XRESOLUTION, 600);
                        output.SetField(TiffTag.YRESOLUTION, 600);
                        output.SetField(TiffTag.ORIENTATION, 1);
                        output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                        output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.INCH);
                        output.SetField(TiffTag.PHOTOMETRIC, 0);
                        output.SetField(TiffTag.COMPRESSION, Compression.CCITT_T6);
                        output.SetField(TiffTag.FILLORDER, 1);
                        output.WriteRawStrip(0, compressedData, compressedData.Length);
                        

                        output.CheckpointDirectory();
                        long streamSize = output.GetStream().Size(output.Clientdata());                                                
                        ms.Position = 0;
                        Cgm_Elements.Last().rasterImage = new Bitmap(ms);
                        Cgm_Elements.Last().rasterImage.MakeTransparent(Color.White);
                        output.Close();
                        ms.Close();
                        
                        //Cgm_Elements.Last().rasterImage = new Bitmap(filename + ".tif");
                        //Cgm_Elements.Last().rasterImage.MakeTransparent(Color.White);

                    }
                    #endregion
                } 
                #endregion
            }
            else if (elemName == "PATTERN TABLE")
            {
                #region MyRegion
                int bytesRead = 0;
                buffer = new byte[Cgm_Elements.Last().idx_precision / 8];

                bytesRead += br.Read(buffer, 0, buffer.Length);
                int p_idx = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int nx = (int)Cgm_Elements.Last().bytes_getValue_uint(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int ny = (int)Cgm_Elements.Last().bytes_getValue_uint(buffer, Cgm_Elements.Last().integer_precision);

                buffer = new byte[Cgm_Elements.Last().integer_precision / 8];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int precision = (int)Cgm_Elements.Last().bytes_getValue_uint(buffer, Cgm_Elements.Last().integer_precision);
                precision = precision == 0 ? Cgm_Elements.Last().colour_idx_precision : precision;
                bool directColor = Cgm_Elements.Last().colourSelectionMode == "indexed colour mode";
                
                bytesRead = paramLen - bytesRead;

                int pixels = bytesRead / (precision/8);

                
                List<Color> pixelList = new List<Color>();
                while (pixels > 0)
                {                    
                    Color c = Cgm_Elements.Last().getColor(precision, br);
                    pixelList.Add(c);
                    pixels -= 1;
                }

                #region Create Bitmap
                Cgm_Elements.Last().rasterImage = new Bitmap((int)nx, (int)ny);
                Graphics g = Graphics.FromImage(Cgm_Elements.Last().rasterImage);

                int k = 0;
                for (int i = 0; i < ny; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        Color pixel_c = pixelList[k];
                        Cgm_Elements.Last().rasterImage.SetPixel(j, i, pixel_c);
                        k++;
                    }
                }

                #endregion
                #endregion

            }
            else if (elemName == "PATTERN INDEX")
            {
                #region MyRegion
                int bytesRead = 0;
                buffer = new byte[paramLen];

                bytesRead += br.Read(buffer, 0, buffer.Length);
                int p_idx = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, buffer.Length * 8);
                Cgm_Elements.Last().pattern_idx = p_idx.ToString();
                #endregion
            }
            else if (elemName == "POLYGON SET")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int b_len = p / 8;
                int words_cnt = paramLen / ((b_len * 2) + 2);
                while (words_cnt > 0)
                {
                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);


                    buffer = new byte[Cgm_Elements.Last().idx_precision/8];
                    br.Read(buffer, 0, buffer.Length);
                    string polygonFlag = "";
                    switch (buffer.Last())
                    {
                        case 0:
                            polygonFlag = "invisible";
                            break;
                        case 1:
                            polygonFlag = "visible";
                            break;
                        case 2:
                            polygonFlag = "close,invisible";
                            break;
                        case 3:
                            polygonFlag = "close,visible";
                            break;
                    }
                    Cgm_Elements.Last().polygonSetFlags.Add(polygonFlag);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;

                }
                #endregion
            }

            else if (elemName == "POLYGON")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int b_len = p / 8;
                int words_cnt = paramLen / (b_len * 2);
                buffer = new byte[paramLen];

                while (words_cnt > 0)
                {
                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);

                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });
                    words_cnt = words_cnt - 1;
                }


                #endregion

            }
            else if (elemName == "POLYLINE")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int points_cnt = (paramLen) / (byteLen * 2);
                while (points_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float px = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float py = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });
                    points_cnt = points_cnt - 1;
                }
                #endregion
            }
            else if (elemName == "POLYBEZIER")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().polybezier_continuous = buffer.Last();
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int points_cnt = (paramLen - bcnt) / (byteLen * 2);
                while (points_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float px = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    points_cnt = points_cnt - 1;
                }
                #endregion
            }

            else if (elemName == "POLYMARKER")
            {
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
            }

            else if (elemName == "CIRCLE")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = (paramLen) / (byteLen * 3);
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    words_cnt = words_cnt - 1;

                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float radius = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));
                    Cgm_Elements.Last().points.Add(new PointF(radius, radius));
                    words_cnt = words_cnt - 1;
                    Cgm_Elements.Last().arc_radius = radius;
                }
                #endregion
            }
            else if (elemName == "CIRCULAR ARC 3 POINT")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = (paramLen) / (byteLen * 2);
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));
                    words_cnt = words_cnt - 1;
                }

                PointF intecection = solve2Pt_arc_c(Cgm_Elements.Last().points[0], Cgm_Elements.Last().points[1], Cgm_Elements.Last().points[2], (int)Cgm_Elements.Last().page_width);
                float radius = (float)distance_180(intecection, Cgm_Elements.Last().points[0]);
                Cgm_Elements.Last().points.Add(intecection);
                Cgm_Elements.Last().points.Add(new PointF((float)radius, (float)radius));
                Cgm_Elements.Last().arc_radius = radius;
                #endregion
            }
            else if (elemName == "CIRCULAR ARC 3 POINT CLOSE")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = (paramLen - (2)) / (byteLen * 2);
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);


                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));
                    words_cnt = words_cnt - 1;
                }

                buffer = new byte[2];

                br.Read(buffer, 0, buffer.Length);

                float closure = Cgm_Elements.Last().bytes_getValue_int(buffer, 16);

                Cgm_Elements.Last().cir_arc_closure = closure == 0 ? "pieclosure" : "chord closure";

                PointF intecection = solve2Pt_arc_c(Cgm_Elements.Last().points[0], Cgm_Elements.Last().points[1], Cgm_Elements.Last().points[2], (int)Cgm_Elements.Last().page_width);

                double radius = distance_180(intecection, Cgm_Elements.Last().points[0]);

                Cgm_Elements.Last().points.Add(intecection);

                Cgm_Elements.Last().points.Add(new PointF((float)radius, (float)radius));
                Cgm_Elements.Last().arc_radius = (float)radius;
                #endregion
            }
            else if (elemName == "CIRCULAR ARC CENTRE")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = ((paramLen - byteLen) / (byteLen * 2));
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);
                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));



                    words_cnt = words_cnt - 1;

                }


                buffer = new byte[byteLen];
                br.Read(buffer, 0, buffer.Length);
                float radius = Cgm_Elements.Last().bytes_getValue(buffer, precision);
                Cgm_Elements.Last().points.Add(new PointF(radius, radius));
                Cgm_Elements.Last().arc_radius = radius;


                double d = Cgm_Elements.Last().points[3].X;
                double a;
                float m, b;
                PointF pt_x = PointF.Add(Cgm_Elements.Last().points[0], new SizeF(Cgm_Elements.Last().points[1]));
                distance_180(Cgm_Elements.Last().points[0], pt_x, out  a, out m, out b);

                pt_x.X = Cgm_Elements.Last().points[0].X + (float)(d * Math.Cos(a * Math.PI / 180));
                pt_x.Y = Cgm_Elements.Last().points[0].Y + (float)(d * Math.Sin(a * Math.PI / 180));

                Cgm_Elements.Last().points[1] = pt_x;


                pt_x = PointF.Add(Cgm_Elements.Last().points[0], new SizeF(Cgm_Elements.Last().points[2]));
                distance_180(Cgm_Elements.Last().points[0], pt_x, out  a, out m, out b);

                pt_x.X = (float)Math.Round(Cgm_Elements.Last().points[0].X + (float)(d * Math.Cos(a * Math.PI / 180)), 3);
                pt_x.Y = (float)Math.Round(Cgm_Elements.Last().points[0].Y + (float)(d * Math.Sin(a * Math.PI / 180)), 3);

                Cgm_Elements.Last().points[2] = pt_x;
                if (Cgm_Elements.Last().points[1] == Cgm_Elements.Last().points[2])
                {
                    a = 0;
                    pt_x.Y -= 0.001f;
                    Cgm_Elements.Last().points[2] = pt_x;
                    Cgm_Elements.Last().isCircle = true;
                }
                #endregion

            }
            else if (elemName == "CIRCULAR ARC CENTRE CLOSE")
            {
                #region MyRegion
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int words_cnt = ((paramLen - (2 + byteLen)) / (byteLen * 2));
                while (words_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);


                    float cx = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float cy = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    Cgm_Elements.Last().points.Add(new PointF(cx, cy));
                    words_cnt = words_cnt - 1;

                }


                buffer = new byte[byteLen];
                br.Read(buffer, 0, buffer.Length);
                float radius = Cgm_Elements.Last().bytes_getValue(buffer, precision);
                Cgm_Elements.Last().points.Add(new PointF(radius, radius));

                Cgm_Elements.Last().arc_radius = radius;

                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                float closure = Cgm_Elements.Last().bytes_getValue_int(buffer, 16);
                Cgm_Elements.Last().cir_arc_closure = closure == 0 ? "pieclosure" : "chord closure";




                double d = Cgm_Elements.Last().points[3].X;
                double a;
                float m, b;
                PointF pt_x = PointF.Add(Cgm_Elements.Last().points[0], new SizeF(Cgm_Elements.Last().points[1]));
                distance_180(Cgm_Elements.Last().points[0], pt_x, out  a, out m, out b);

                pt_x.X = Cgm_Elements.Last().points[0].X + (float)(d * Math.Cos(a * Math.PI / 180));
                pt_x.Y = Cgm_Elements.Last().points[0].Y + (float)(d * Math.Sin(a * Math.PI / 180));

                Cgm_Elements.Last().points[1] = pt_x;


                pt_x = PointF.Add(Cgm_Elements.Last().points[0], new SizeF(Cgm_Elements.Last().points[2]));
                distance_180(Cgm_Elements.Last().points[0], pt_x, out  a, out m, out b);

                pt_x.X = (float)Math.Round(Cgm_Elements.Last().points[0].X + (float)(d * Math.Cos(a * Math.PI / 180)), 3);
                pt_x.Y = (float)Math.Round(Cgm_Elements.Last().points[0].Y + (float)(d * Math.Sin(a * Math.PI / 180)), 3);

                Cgm_Elements.Last().points[2] = pt_x;

                #endregion
            }

            else if (elemName == "CIRCULAR ARC CENTRE REVERSED")
            {
                #region MyRegion
                int words_cnt = 3;
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;
                }
                buffer = new byte[buf_len];
                br.Read(buffer, 0, buffer.Length);

                float radius = BitConverter.ToSingle(buffer, 0);
                #endregion
            }

            else if (elemName == "ELLIPSE")
            {
                #region MyRegion

                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                int words_cnt = paramLen / (buf_len * 2);
                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);


                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);


                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);


                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;
                }
                #endregion
            }
            else if (elemName == "ELLIPTICAL ARC")
            {
                int sigDigits = 3;
                #region MyRegion
                int vdc_cnt = 3;
                int precision = Cgm_Elements.Last().getPrecision_vdc();
                int byteLen = precision / 8;
                while (vdc_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float px = 0;

                    px = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, precision), sigDigits);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    float py = 0;



                    py = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, precision), sigDigits);
                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });
                    vdc_cnt = vdc_cnt - 1;
                }

                vdc_cnt = 2;

                while (vdc_cnt > 0)
                {
                    float px, py;

                    buffer = new byte[byteLen];

                    br.Read(buffer, 0, buffer.Length);

                    px = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, precision), sigDigits);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    py = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, precision), sigDigits);


                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });
                    vdc_cnt = vdc_cnt - 1;
                }
                #endregion

            }
            else if (elemName == "ELLIPTICAL ARC CLOSE")
            {
                #region MyRegion
                int words_cnt = 5;
                int sigDigits = 3;
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;


                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);



                    float px = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, p), sigDigits);

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);


                    float py = (float)Math.Round(Cgm_Elements.Last().bytes_getValue(buffer, p), sigDigits);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;
                }
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().cir_arc_closure = buffer[1] == 0 ? "pieclosure" : "chord closure";

                #endregion
            }


            else if (elemName == "HYPERBOLIC ARC")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                int words_cnt = 5;
                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;
                }
                #endregion

            }
            else if (elemName == "PARABOLIC ARC")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                int words_cnt = paramLen / (2 * buf_len);
                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;
                }
                PointF[] pts = Cgm_Elements.Last().points.ToArray();
                PointF V1 = PointF.Subtract(pts[1], new SizeF(pts[0].X, pts[0].Y));
                PointF V2 = PointF.Subtract(pts[2], new SizeF(pts[0].X, pts[0].Y));

                float[,] x = {
                              { V1.X, V2.X },
                              { V1.Y, V2.Y }
                             };
                Matrix<float> M = Matrix<float>.Build.DenseOfArray(x);
                Matrix<float> C = Matrix<float>.Build.DenseOfArray(new float[,] { { pts[0].X }, { pts[0].Y } });
                Matrix<float> P = Matrix<float>.Build.DenseOfArray(new float[,] { { .25f }, { .25f } });

                Matrix<float> PT = (M * P) + C;

                float b, m;
                double a;
                float p_dist = (float)distance_180(pts[0], new PointF(PT[0, 0], PT[1, 0]), out a, out m, out b);

                int sign_x = Math.Sign(Math.Cos(360 + a));
                int sign_y = Math.Sign(Math.Sin(360 + a));

                distance_180(pts[0], pts[1], out a, out m, out b);
                a = a * (Math.PI / 180);
                float xx = (float)(p_dist * Math.Cos(a)) + pts[0].X;
                float yy = (float)(p_dist * Math.Sin(a)) + pts[0].Y;
                Cgm_Elements.Last().points.Add(new PointF(xx, yy));


                distance_180(pts[0], pts[2], out a, out m, out b);
                a = a * (Math.PI / 180);
                xx = (float)(p_dist * Math.Cos(a)) + pts[0].X;
                yy = (float)(p_dist * Math.Sin(a)) + pts[0].Y;
                Cgm_Elements.Last().points.Add(new PointF((float)(p_dist * Math.Cos(a)) + pts[0].X, (float)(p_dist * Math.Sin(a)) + pts[0].Y));


                pts = Cgm_Elements.Last().points.ToArray();


                #endregion

            }
            else if (elemName == "NON-UNIFORM B-SPLINE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "NON-UNIFORM RATIONAL B-SPLINE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "RECTANGLE")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();

                int buf_len = p / 8;
                int words_cnt = paramLen / (buf_len * 2);


                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);


                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);


                    words_cnt = words_cnt - 1;

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });
                }
                Cgm_Elements.Last().width = Math.Abs(Cgm_Elements.Last().points[1].X - Cgm_Elements.Last().points[0].X);
                Cgm_Elements.Last().height = Math.Abs(Cgm_Elements.Last().points[1].Y - Cgm_Elements.Last().points[0].Y);
                #endregion
            }
            else if (elemName == "DISJOINT POLYLINE")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                int words_cnt = paramLen / (buf_len * 2);
                while (words_cnt > 0)
                {
                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float px = Cgm_Elements.Last().bytes_getValue(buffer, p);


                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float py = Cgm_Elements.Last().bytes_getValue(buffer, p);

                    Cgm_Elements.Last().points.Add(new PointF()
                    {
                        X = px,
                        Y = py
                    });

                    words_cnt = words_cnt - 1;

                }
                #endregion
            }
            else if (elemName == "RESTRICTED TEXT")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;


                int ii = (int)paramLen;
                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float delta_width = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());


                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float delta_height = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());

                Cgm_Elements.Last().delta_width = delta_width;
                Cgm_Elements.Last().delta_height = delta_height;


                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float position_x = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());

                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float position_y = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision());

                Cgm_Elements.Last().position = new PointF(position_x, position_y);

                buffer = new byte[2];
                ii -= br.Read(buffer, 0, buffer.Length);
                bool isFinal = buffer.Last() == 1;
                Cgm_Elements.Last().isFinalText = isFinal;

                ii -= br.Read(buffer, 0, 1);

                buffer = new byte[ii];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer).Trim();
                Cgm_Elements.Last().text = str;
                Cgm_Elements.Last().elem_NameAlt = "TEXT";
                #endregion
            }
            else if (elemName == "TEXT")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;


                int ii = (int)paramLen;
                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float position_x = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());


                buffer = new byte[buf_len];
                ii -= br.Read(buffer, 0, buffer.Length);

                float position_y = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());


                Cgm_Elements.Last().position = new PointF(position_x, position_y);

                buffer = new byte[2];
                ii -= br.Read(buffer, 0, buffer.Length);
                bool isFinal = buffer.Last() == 1;
                Cgm_Elements.Last().isFinalText = isFinal;
                //ii -= br.Read(buffer, 0, 1);

                buffer = new byte[ii];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer).Trim();
                Cgm_Elements.Last().text = str;
                Cgm_Elements.Last().elem_NameAlt = "TEXT";
                #endregion
            }
            else if (elemName == "APPEND TEXT")
            {
                #region MyRegion


                int ii = (int)paramLen;
                buffer = new byte[2];
                ii -= br.Read(buffer, 0, buffer.Length);
                bool isFinal = buffer.Last() == 1;
                Cgm_Elements.Last().isFinalText = isFinal;
                buffer = new byte[ii];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer).Trim();
                float charHeight = Cgm_Elements.Last().characterHeight;
                Cgm_Element textElem = Cgm_Elements.Last(fd => fd.elem_NameAlt == "TEXT");
                if (textElem != null)
                {
                    textElem.appendedText.Add(new string[] { charHeight.ToString(), " " + str });
                }
                Cgm_Elements.Last().text = str;
                #endregion
            }


            else if (elemName == "VDC EXTENT")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int buf_len = p / 8;
                int words_cnt = paramLen / (buf_len * 2);
                while (words_cnt > 0)
                {

                    Cgm_Elements.Last().vdcExtent = new PointF[] { new PointF(), new PointF() };

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);

                    float x1 = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().vdc_real_precision);
                    Cgm_Elements.Last().vdcExtent[0].X = x1;

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);

                    float y1 = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().vdc_real_precision);
                    Cgm_Elements.Last().vdcExtent[0].Y = y1;

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float x2 = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().vdc_real_precision);
                    Cgm_Elements.Last().vdcExtent[1].X = x2;

                    buffer = new byte[buf_len];
                    br.Read(buffer, 0, buffer.Length);
                    float y2 = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().vdc_real_precision);
                    Cgm_Elements.Last().vdcExtent[1].Y = y2;

                    Cgm_Elements.Last().isbottomUp = Cgm_Elements.Last().vdcExtent[1].Y > Cgm_Elements.Last().vdcExtent[0].Y;
                    Cgm_Elements.Last().isleftRight = Cgm_Elements.Last().vdcExtent[1].X > Cgm_Elements.Last().vdcExtent[0].X;

                    if ((Cgm_Elements.Last().vdcExtent[1].X > 0) && (Cgm_Elements.Last().vdcExtent[0].X > 0))
                    {
                        Cgm_Elements.Last().page_width = Math.Max(Cgm_Elements.Last().vdcExtent[1].X, Cgm_Elements.Last().vdcExtent[0].X);
                    }
                    else
                    {
                        Cgm_Elements.Last().page_width = Math.Abs(Cgm_Elements.Last().vdcExtent[1].X - Cgm_Elements.Last().vdcExtent[0].X);
                    }

                    if ((Cgm_Elements.Last().vdcExtent[1].Y > 0) && (Cgm_Elements.Last().vdcExtent[0].Y > 0))
                    {
                        Cgm_Elements.Last().page_height = Math.Max(Cgm_Elements.Last().vdcExtent[1].Y, Cgm_Elements.Last().vdcExtent[0].Y);
                    }
                    else
                    {
                        Cgm_Elements.Last().page_height = Math.Abs(Cgm_Elements.Last().vdcExtent[1].Y - Cgm_Elements.Last().vdcExtent[0].Y);
                    }



                    Cgm_Elements.Last().true_height = Cgm_Elements.Last().page_height;
                    Cgm_Elements.Last().true_width = Cgm_Elements.Last().page_width;
                    Cgm_Elements.Last().vdc_idx = vdc_idx++;
                    picture_idx = 0;
                    Cgm_Elements.Last().picture_idx = picture_idx;
                    words_cnt = words_cnt - 2;

                    if (Cgm_Elements.Last().lineSizeMode == "absolute")
                    {
                        //if (Cgm_Elements.Last().lineWidthSet == false)
                        {
                            Cgm_Elements.Last().strokeWidth = Math.Max(Cgm_Elements.Last().page_height, Cgm_Elements.Last().page_width) / 1000;

                        }
                        //if (Cgm_Elements.Last().edgeWidthSet == false)
                        {
                            Cgm_Elements.Last().edgeWidth = Math.Max(Cgm_Elements.Last().page_height, Cgm_Elements.Last().page_width) / 1000;
                        }

                    }
                }
                #endregion
            }
            else if (elemName == "METAFILE DEFAULTS REPLACEMENT")
            {
                #region MyRegion
                int words_cnt = paramLen;
                while (words_cnt > 0)
                {
                    getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);                    
                    parseMetaElement(ref br, ref buffer, ref bytesread, ref paramLen, ref elemclass, ref elemId, ref elemName, ref Cgm_Elements, false);
                    words_cnt -= bytesread;
                }
                #endregion
            }
            else if (elemName == "SCALING MODE")
            {
                #region MyRegion
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                str = buffer[0] == 0 ? "abstract scaling" : "metric scaling";
                buffer = new byte[4];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                float scale = BitConverter.ToSingle(buffer, 0);
                Cgm_Elements.Last().scaleFactor = scale;
                #endregion
            }
            else if (elemName == "COLOUR SELECTION MODE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().colourSelectionMode = buffer.Last() == 0 ? "indexed colour mode" : "direct colour mode";
                if (buffer.Last() == 1)
                {
                    Cgm_Elements.Last().colour_precision = Cgm_Elements.Last().pixel_precision;
                }
                #endregion
            }
            else if (elemName == "COLOUR MODEL")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                string color_model = "";
                switch (buffer.Last())
                {
                    case 1:
                        color_model = "RGB";
                        break;
                    case 2:
                        color_model = "CIELAB";
                        break;
                    case 3:
                        color_model = "CIELUV slope";
                        break;
                    case 4:
                        color_model = "CMYK";
                        break;
                    case 5:
                        color_model = "RGB-related";
                        break;
                    default:
                        color_model = "reserved for registered values";
                        break;
                }
                Cgm_Elements.Last().color_model = color_model;
                Cgm_Elements.Last().color_model_idx = buffer.Last().ToString();

                #endregion
            }
            else if (elemName == "COLOUR VALUE EXTENT")
            {
                #region MyRegion
                if (paramLen == 6)
                {
                    Cgm_Elements.Last().colour_value_extent = "RGB/CMYK";
                    Cgm_Elements.Last().colour_value_extent_size = paramLen / 2;
                    buffer = new byte[3];
                    br.Read(buffer, 0, buffer.Length);

                    Color cc = Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);
                    Cgm_Elements.Last().min_rgb = cc;
                    buffer = new byte[3];
                    br.Read(buffer, 0, buffer.Length);
                    cc = Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);
                    Cgm_Elements.Last().max_rgb = cc;
                    Cgm_Elements.Last().pixel_precision = 8 *  (paramLen / 2);
                }
                else if (paramLen == 12)
                {
                    buffer = new byte[paramLen];
                    br.Read(buffer, 0, buffer.Length);
                }

                #endregion
            }
            else if (elemName == "COLOUR TABLE")
            {
                #region MyRegion

                buffer = new byte[Cgm_Elements.Last().colour_idx_precision / 8];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().colourTable_start_idx = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().colour_idx_precision);

                while (Cgm_Elements.Last().colorTable.Count < Cgm_Elements.Last().colourTable_start_idx)
                {
                    Cgm_Elements.Last().colorTable.Add(Color.FromArgb(255, 255, 255, 255));
                }

                List<Color> nwColorTable = new List<Color>();
                buffer = new byte[paramLen - buffer.Length];
                if (buffer.Length != 0)
                {

                    br.Read(buffer, 0, buffer.Length);
                    int st_idx = buffer[0];
                    if (Cgm_Elements.Last().colour_precision == 16)
                    {
                        int[] idx_breaks = Enumerable.Range(0, buffer.Length / 6).Select(x => x * 6).ToArray();
                        nwColorTable = (idx_breaks.Select(i => Color.FromArgb(255, buffer[i + 1], buffer[i + 3], buffer[i + 5])).ToList());
                    }
                    else if (Cgm_Elements.Last().colour_precision == 8)
                    {
                        int[] idx_breaks = Enumerable.Range(0, buffer.Length / 3).Select(x => x * 3).ToArray();
                        if (buffer.Length % 3 == 0)
                        {
                            nwColorTable = (idx_breaks.Select(i => Color.FromArgb(255, buffer[i + 0], buffer[i + 1], buffer[i + 2])).ToList());
                        }
                        else
                        {
                            nwColorTable = (idx_breaks.Select(i => Color.FromArgb(255, buffer[i + 1], buffer[i + 2], buffer[i + 3])).ToList());
                        }

                    }
                    else if (Cgm_Elements.Last().colorModel == "RGB")
                    {
                        int[] idx_breaks = Enumerable.Range(0, buffer.Length / 3).Select(x => x * 3).ToArray();
                        nwColorTable = (idx_breaks.Select(i => Color.FromArgb(255, buffer[i], buffer[i + 1], buffer[i + 2])).ToList());
                    }
                    else if (Cgm_Elements.Last().colorModel == "CMYK")
                    {
                        int[] idx_breaks = Enumerable.Range(0, buffer.Length / 4).Select(x => x * 4).ToArray();
                        nwColorTable = (idx_breaks.Select(i => Color.FromArgb(buffer[i + 3], buffer[i], buffer[i + 1], buffer[i + 2])).ToList());
                    }

                    Cgm_Elements.Last().colorTable.InsertRange(Cgm_Elements.Last().colourTable_start_idx, nwColorTable);
                }

                #endregion
            }
            else if (elemName == "COLOUR PRECISION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().pixel_precision = 3 * (int)Cgm_Elements.Last().bytes_getValue_int(buffer, paramLen * 8);
                Cgm_Elements.Last().colour_precision =  (int)Cgm_Elements.Last().bytes_getValue_int(buffer, paramLen * 8);
                #endregion
            }
            else if (elemName == "COLOUR INDEX PRECISION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().colour_idx_precision = buffer.Last();
                #endregion
            }
            else if (elemName == "INTERIOR STYLE SPECIFICATION MODE")
            {
                #region MyRegion
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                switch (buffer[0])
                {
                    case 0:
                        str = "absolute";
                        break;
                    case 1:
                        str = "scaled";
                        break;
                    case 2:
                        str = "fractional";
                        break;
                    case 3:
                        str = "mm";
                        break;
                }
                #endregion
            }

            else if (elemName == "HATCH INDEX")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                string hatch_style = "";
                switch (buffer.Last())
                {
                    case 1:
                        hatch_style = "horizontal";
                        break;
                    case 2:
                        hatch_style = "vertical";
                        break;
                    case 3:
                        hatch_style = "positive slope";
                        break;
                    case 4:
                        hatch_style = "negative slope";
                        break;
                    case 5:
                        hatch_style = "horizontal/vertical crosshatch";
                        break;
                    case 6:
                        hatch_style = "positive/negative slope crosshatch";
                        break;
                    default:
                        hatch_style = "horizontal/vertical crosshatch";
                        break;
                }
                Cgm_Elements.Last().hatch_style = hatch_style;
                Cgm_Elements.Last().hatch_id = buffer.Last().ToString();

                #endregion
            }
            else if (elemName == "INTERIOR STYLE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                string fill_style = "";
                switch (buffer.Last())
                {
                    case 0:
                        fill_style = "hollow";
                        break;
                    case 1:
                        fill_style = "solid";
                        break;
                    case 2:
                        fill_style = "pattern";
                        break;
                    case 3:
                        fill_style = "hatch";
                        break;
                    case 4:
                        fill_style = "empty";
                        break;
                    case 5:
                        fill_style = "geometric pattern";
                        break;
                    case 6:
                        fill_style = "interpolated";
                        break;
                }
                Cgm_Elements.Last().fill_style = fill_style;
                #endregion
            }
            else if (elemName == "FILL REPRESENTATION")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float fill_idx = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float interior_style = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                int p = Cgm_Elements.Last().colour_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                Color fill_color = new Color();
                Cgm_Elements.Last().pixelColor(buffer, ref fill_color, p);

                bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float hatch_idx = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);


                bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float patten_idx = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                #endregion
            }
            else if (elemName == "TEXT REPRESENTATION")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float text_idx = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float text_type = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                buffer = new byte[1];
                br.Read(buffer, 0, buffer.Length);
                float text_precision = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);



                int p = Cgm_Elements.Last().real_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float character_spacing = Cgm_Elements.Last().bytes_getValue_real(buffer, p);



                p = Cgm_Elements.Last().real_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float character_expansion_factor = Cgm_Elements.Last().bytes_getValue_real(buffer, p);



                p = Cgm_Elements.Last().colour_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                Color lineColor = new Color();
                Cgm_Elements.Last().pixelColor(buffer, ref lineColor, p);
                #endregion
            }
            else if (elemName == "MARKER REPRESENTATION")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float marker_idx = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float marker_type = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);

                int p = Cgm_Elements.Last().getPrecision_vdc();
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                float size = Cgm_Elements.Last().bytes_getValue_real(buffer, p);

                p = Cgm_Elements.Last().colour_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                Color lineColor = new Color();
                Cgm_Elements.Last().pixelColor(buffer, ref lineColor, p);
                #endregion
            }
            else if (elemName == "LINE REPRESENTATION")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float line_bundle_index = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);


                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float line_type = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);


                int p = Cgm_Elements.Last().getPrecision_vdc();
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                float size = Cgm_Elements.Last().bytes_getValue_real(buffer, p);

                p = Cgm_Elements.Last().colour_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                Color lineColor = new Color();
                Cgm_Elements.Last().pixelColor(buffer, ref lineColor, p);

                #endregion
            }
            else if (elemName == "EDGE REPRESENTATION")
            {
                #region MyRegion
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float edge_bundle_index = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);


                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                float edge_type = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);


                int p = Cgm_Elements.Last().getPrecision_vdc();
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                float size = Cgm_Elements.Last().bytes_getValue_real(buffer, p);

                p = Cgm_Elements.Last().colour_precision;
                bcnt = p / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);

                Color lineColor = new Color();
                Cgm_Elements.Last().pixelColor(buffer, ref lineColor, p);
                #endregion
            }
            else if (elemName == "LINE AND EDGE TYPE DEFINITION")
            {

                #region MyRegion
                Cgm_Elements.Last().lineEdgeDefs = new LineEdgeType();

                Cgm_Elements.Last().lineEdgeDefs.dashseq = new List<float>();


                int readBytes = 0;
                int p = Cgm_Elements.Last().idx_precision;
                int bcnt = Cgm_Elements.Last().idx_precision / 8;
                buffer = new byte[bcnt];
                readBytes += br.Read(buffer, 0, buffer.Length);
                float id = Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);
                Cgm_Elements.Last().lineEdgeDefs.id = (int)id;


                p = Cgm_Elements.Last().getPrecision_vdc();
                bcnt = p / 8;
                buffer = new byte[bcnt];
                readBytes += br.Read(buffer, 0, buffer.Length);
                float len = Cgm_Elements.Last().bytes_getValue(buffer, p);
                    
                    
                Cgm_Elements.Last().lineEdgeDefs.dashCycle_Length = len;

                p = Cgm_Elements.Last().integer_precision;
                int b_len = p / 8;
                int dash_cnt = (paramLen - readBytes) / b_len;
                while (dash_cnt > 0)
                {
                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);
                    Cgm_Elements.Last().lineEdgeDefs.dashseq.Add((int)Cgm_Elements.Last().bytes_getValue_int(buffer, p));
                    dash_cnt -= 1;
                }
                float factor = len / Cgm_Elements.Last().lineEdgeDefs.dashseq.Sum();
                Cgm_Elements.Last().lineEdgeDefs.dashseq = Cgm_Elements.Last().lineEdgeDefs.dashseq.Select(fd => fd * factor).ToList();
                
                lineEdgeTypeLookUp.Add(Cgm_Elements.Last().lineEdgeDefs);
                #endregion
            }
            else if (elemName == "HATCH STYLE DEFINITION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "GEOMETRIC PATTERN DEFINITION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "LINE WIDTH SPECIFICATION MODE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                switch (buffer[0])
                {
                    case 0:
                        str = "absolute";
                        break;
                    case 1:
                        str = "scaled";
                        break;
                    case 2:
                        str = "fractional";
                        break;
                    case 3:
                        str = "mm";
                        break;
                }
                Cgm_Elements.Last().lineSizeMode = str;
                if (Cgm_Elements.Last().lineWidthSet == false)
                {
                    if (str == "absolute")
                    {
                        Cgm_Elements.Last().strokeWidth = Math.Max(Cgm_Elements.Last().page_height, Cgm_Elements.Last().page_width) / 1000;
                    }
                    else if (str == "mm")
                    {
                        Cgm_Elements.Last().strokeWidth = 0.35f;
                    }
                    else if (str == "fractional")
                    {
                        Cgm_Elements.Last().strokeWidth = 0.001f;
                    }
                }                
                #endregion
            }
            else if (elemName == "MARKER SIZE SPECIFICATION MODE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                switch (buffer[0])
                {
                    case 0:
                        str = "absolute";
                        break;
                    case 1:
                        str = "scaled";
                        break;
                    case 2:
                        str = "fractional";
                        break;
                    case 3:
                        str = "mm";
                        break;
                }
                Cgm_Elements.Last().markerSizeMode = str;
                #endregion
            }
            else if (elemName == "EDGE WIDTH SPECIFICATION MODE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                switch (buffer[0])
                {
                    case 0:
                        str = "absolute";
                        break;
                    case 1:
                        str = "scaled";
                        break;
                    case 2:
                        str = "fractional";
                        break;
                    case 3:
                        str = "mm";
                        break;
                }
                Cgm_Elements.Last().edgeSizeMode = str;
                if (Cgm_Elements.Last().edgeWidthSet == false)
                {
                    if (str == "absolute")
                    {
                        Cgm_Elements.Last().edgeWidth = Math.Max(Cgm_Elements.Last().page_height, Cgm_Elements.Last().page_width) / 1000;
                    }
                    else if (str == "mm")
                    {
                        Cgm_Elements.Last().edgeWidth = 0.35f;
                    }
                    else if (str == "fractional")
                    {
                        Cgm_Elements.Last().edgeWidth = 0.001f;
                    }
                }                
                #endregion
            }
            else if (elemName == "LINE JOIN")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                int ff = br.Read(buffer, 0, buffer.Length);

                Int16 opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string line_j = "";
                switch (opc)
                {
                    case 0:
                        line_j = "round";
                        break;
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "mitre";
                        break;
                    case 3:
                        line_j = "round";
                        break;
                    case 4:
                        line_j = "bevel";
                        break;
                    default:
                        line_j = "round";
                        break;
                };
                Cgm_Elements.Last().lineJoin = line_j;
                #endregion
            }
            else if (elemName == "EDGE JOIN")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                int ff = br.Read(buffer, 0, buffer.Length);

                Int16 opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string line_j = "";
                switch (opc)
                {
                    case 0:
                        line_j = "round";
                        break;
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "mitre";
                        break;
                    case 3:
                        line_j = "round";
                        break;
                    case 4:
                        line_j = "bevel";
                        break;
                    default:
                        line_j = "round";
                        break;
                };
                Cgm_Elements.Last().edgeJoin = line_j;
                #endregion
            }
            else if (elemName == "LINE CAP")
            {
                #region MyRegion
                buffer = new byte[paramLen / 2];
                int ff = br.Read(buffer, 0, buffer.Length);
                Int16 opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string line_j = "";
                switch (opc)
                {
                    case 0:
                        line_j = "round";
                        break;
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "butt";
                        break;
                    case 3:
                        line_j = "round";
                        break;
                    case 4:
                        line_j = "projecting square";
                        break;
                    case 5:
                        line_j = "triangle";
                        break;
                    default:
                        line_j = "round";
                        break;
                }

                Cgm_Elements.Last().lineCap = line_j;

                buffer = new byte[paramLen / 2];
                ff = br.Read(buffer, 0, buffer.Length);
                opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                line_j = "";
                switch (opc)
                {
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "butt";
                        break;
                    case 3:
                        line_j = "match";
                        break;
                    default:
                        line_j = "butt";
                        break;
                }
                Cgm_Elements.Last().dashCapIndicator = line_j;
                #endregion
            }
            else if (elemName == "EDGE CAP")
            {
                #region MyRegion
                buffer = new byte[paramLen / 2];
                int ff = br.Read(buffer, 0, buffer.Length);
                Int16 opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string line_j = "";
                switch (opc)
                {
                    case 0:
                        line_j = "round";
                        break;
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "butt";
                        break;
                    case 3:
                        line_j = "round";
                        break;
                    case 4:
                        line_j = "projecting square";
                        break;
                    case 5:
                        line_j = "triangle";
                        break;
                    default:
                        line_j = "round";
                        break;
                }

                Cgm_Elements.Last().edgeCap = line_j;

                buffer = new byte[paramLen / 2];
                ff = br.Read(buffer, 0, buffer.Length);
                opc = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                line_j = "";
                switch (opc)
                {
                    case 1:
                        line_j = "unspecified";
                        break;
                    case 2:
                        line_j = "butt";
                        break;
                    case 3:
                        line_j = "match";
                        break;
                    default:
                        line_j = "butt";
                        break;
                }
                Cgm_Elements.Last().edgedashCapIndicator = line_j;
                #endregion
            }
            else if (elemName == "EDGE TYPE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Int16 edge_t = paramLen == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string edgeType = "";
                switch (edge_t)
                {
                    case 0:
                        edgeType = "solid";
                        break;
                    case 1:
                        edgeType = "solid";
                        break;
                    case 2:
                        edgeType = "dash";
                        break;
                    case 3:
                        edgeType = "dot";
                        break;
                    case 4:
                        edgeType = "dash-dot";
                        break;
                    case 5:
                        edgeType = "dash-dot-dot";
                        break;
                    default:
                        edgeType = edge_t.ToString();
                        break;
                }
                Cgm_Elements.Last().edgeType = edge_t.ToString();
                #endregion
            }
            else if (elemName == "LINE TYPE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);

                Int16 edge_t = paramLen == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string edgeType = "";
                switch (edge_t)
                {
                    case 0:
                        edgeType = "solid";
                        break;
                    case 1:
                        edgeType = "solid";
                        break;
                    case 2:
                        edgeType = "dash";
                        break;
                    case 3:
                        edgeType = "dot";
                        break;
                    case 4:
                        edgeType = "dash-dot";
                        break;
                    case 5:
                        edgeType = "dash-dot-dot";
                        break;
                    default:
                        edgeType = edge_t.ToString();
                        break;
                }
                //Cgm_Elements.Last().strokeWidth = 0.5f;
                Cgm_Elements.Last().lineType = edge_t.ToString();
                #endregion
            }
            else if (elemName == "MITRE LIMIT")
            {
                #region MyRegion

                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);

                float f = Cgm_Elements.Last().bytes_getValue_real(buffer, Cgm_Elements.Last().real_precision);
                Cgm_Elements.Last().mitreLimit = f;
                #endregion

            }
            else if (elemName == "LINE TYPE CONTINUATION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Int16 edge_t = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                string setting = "";
                switch (edge_t)
                {
                    case 0:
                        setting = "unspecified";
                        break;
                    case 1:
                        setting = "unspecified";
                        break;
                    case 2:
                        setting = "continue";
                        break;
                    case 3:
                        setting = "restart";
                        break;
                    case 4:
                        setting = "adaptive continue";
                        break;
                    default:
                        setting = "reserved for registered values";
                        break;
                }

                Cgm_Elements.Last().lineTypeContinue = setting;
                #endregion
            }
            else if (elemName == "LINE TYPE INITIAL OFFSET")
            {
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                float line_pattern_offset = Cgm_Elements.Last().bytes_getValue_real(buffer, Cgm_Elements.Last().real_precision);
            }
            else if (elemName == "EDGE TYPE INITIAL OFFSET")
            {
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                float edge_pattern_offset = Cgm_Elements.Last().bytes_getValue_real(buffer, Cgm_Elements.Last().real_precision);
            }

            else if (elemName == "INDEX PRECISION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().idx_precision = buffer.Last();
                #endregion
            }

            else if (elemName == "BACKGROUND COLOUR")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref Cgm_Elements.Last().bgColor);
                #endregion
            }
            else if (elemName == "FILL COLOUR")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref Cgm_Elements.Last().fillColor);
                #endregion
            }
            else if (elemName == "LINE COLOUR")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref Cgm_Elements.Last().strokeColor);
                #endregion
            }
            else if (elemName == "EDGE COLOUR")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref Cgm_Elements.Last().edgeColor);
                #endregion
            }
            else if (elemName == "TEXT COLOUR")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().extractColor(buffer, ref Cgm_Elements.Last().characterColor);
                #endregion
            }
            else if (elemName == "LINE WIDTH")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                float f = Cgm_Elements.Last().bytes_getValue_edge(buffer);
                Cgm_Elements.Last().strokeWidth = f;
                //if (Cgm_Elements.Last().lineSizeMode == "absolute")
                //{
                //    Cgm_Elements.Last().strokeWidth =  f / 1000;
                //}
                //if (Cgm_Elements.Last().lineSizeMode == "scaled")
                //{
                //    Cgm_Elements.Last().strokeWidth = 10 * f;
                //}
                //else if (Cgm_Elements.Last().lineSizeMode == "fractional")
                //{
                //    Cgm_Elements.Last().strokeWidth = Cgm_Elements.Last().page_width * f;
                //}
                Cgm_Elements.Last().lineWidthSet = true;
                #endregion
            }


            else if (elemName == "EDGE WIDTH")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                float f = Cgm_Elements.Last().bytes_getValue_edge(buffer);
                Cgm_Elements.Last().edgeWidth = f;
                //if (Cgm_Elements.Last().edgeSizeMode == "scaled")
                //{
                //    Cgm_Elements.Last().edgeWidth = 10 * f;
                //}
                //else if (Cgm_Elements.Last().lineSizeMode == "fractional")
                //{
                //    Cgm_Elements.Last().edgeWidth = Cgm_Elements.Last().page_width * f;
                //}
                Cgm_Elements.Last().lineWidthSet = true;
                #endregion
            }
            else if (elemName == "CHARACTER SPACING")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, paramLen);
                #endregion
            }
            else if (elemName == "CHARACTER HEIGHT")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, paramLen);
                float f = Cgm_Elements.Last().bytes_getValue(buffer, Cgm_Elements.Last().getPrecision_vdc());
                Cgm_Elements.Last().characterHeight = f;
                #endregion
            }
            else if (elemName == "CHARACTER ORIENTATION")
            {
                #region MyRegion
                int p = Cgm_Elements.Last().getPrecision();
                int b_len = p / 8;

                buffer = new byte[b_len];
                br.Read(buffer, 0, b_len);

                float xUp = Cgm_Elements.Last().bytes_getValue(buffer, p);

                buffer = new byte[b_len];
                br.Read(buffer, 0, b_len);

                float yUp = Cgm_Elements.Last().bytes_getValue(buffer, p);

                buffer = new byte[b_len];
                br.Read(buffer, 0, b_len);

                float xBase = Cgm_Elements.Last().bytes_getValue(buffer, p);

                buffer = new byte[b_len];
                br.Read(buffer, 0, b_len);

                float YBase = Cgm_Elements.Last().bytes_getValue(buffer, p);

                Cgm_Elements.Last().characterOrientation = new float[] { xUp, yUp, xBase, YBase };
                #endregion
            }
            else if (elemName == "EDGE VISIBILITY")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().edgeVisibility = buffer[1] > 0;
                #endregion
            }
            else if (elemName == "TEXT FONT INDEX")
            {
                #region MyRegion
                buffer = new byte[Cgm_Elements.Last().idx_precision / 8];
                br.Read(buffer, 0, buffer.Length);
                int fontIndex = (int)Cgm_Elements.Last().bytes_getValue_int(buffer, Cgm_Elements.Last().idx_precision);
                Cgm_Elements.Last().fontfamily = Cgm_Elements.Last().fontlist_LIST[fontIndex - 1];
                #endregion
            }
            else if (elemName == "FONT LIST")
            {

                FontFamily[] fontFamilies;

                InstalledFontCollection installedFontCollection = new InstalledFontCollection();

                // Get the array of FontFamily objects.
                fontFamilies = installedFontCollection.Families;
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 31 ? (byte)',' : fd).ToArray();

                str = Encoding.UTF8.GetString(buffer).Trim(new[] { ',', ' ' }); ;
                str = str.Replace("Bold", ";font-weight:bold");
                str = str.Replace("Italic", ";font-style:italic");

                Cgm_Elements.Last().fontlist = str.Trim();
                Cgm_Elements.Last().fontlist_LIST = str.Trim().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();


                #endregion
            }
            else if (elemName == "BEGIN METAFILE")
            {
                #region MyRegion
                if (paramLen > 0)
                {
                    buffer = new byte[paramLen];
                    //br.Read(buffer, 0, 1);
                    br.Read(buffer, 0, buffer.Length);
                    buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                    str = System.Text.Encoding.Default.GetString(buffer);

                }
                #endregion
            }
            else if (elemName == "BEGIN PICTURE")
            {
             
                #region MyRegion
                if (paramLen > 0)
                {
                    if (paramLen > 1)
                    {
                        buffer = new byte[paramLen - 1];
                        br.Read(buffer, 0, 1);
                        br.Read(buffer, 0, buffer.Length);
                        buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                        str = System.Text.Encoding.Default.GetString(buffer);
                    }
                    else
                    {
                        buffer = new byte[paramLen];
                        br.Read(buffer, 0, 1);
                        str = "";
                    }
                }
                #endregion
            }
            else if (elemName == "BEGIN PICTURE BODY")
            {
                Cgm_Elements.Last().picture_idx = picture_idx++;
                #region MyRegion
                if (paramLen > 0)
                {
                    if (paramLen > 1)
                    {
                        buffer = new byte[paramLen - 1];
                        br.Read(buffer, 0, 1);
                        br.Read(buffer, 0, buffer.Length);
                        buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                        str = System.Text.Encoding.Default.GetString(buffer);
                    }
                    else
                    {
                        buffer = new byte[paramLen];
                        br.Read(buffer, 0, 1);
                        str = "";
                    }
                }
                #endregion
            }
            else if (elemName == "END PICTURE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Where(fd => fd >= 32).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer);

                #endregion
            }
            else if (elemName == "BEGIN FIGURE")
            {
                #region MyRegion
                Cgm_Elements.Last().isFig = true;
                Cgm_Elements.Last().start_fig = true;
                #endregion
            }
            else if (elemName == "END FIGURE")
            {
                #region MyRegion
                Cgm_Elements.Last().isFig = false;
                Cgm_Elements.Last().end_fig = true;
                #endregion
            }

            else if (elemName == "METAFILE VERSION")
            {

                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "METAFILE DESCRIPTION")
            {
                #region MyRegion
                buffer = new byte[paramLen - 1];
                br.Read(buffer, 0, 1);
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer);
                #endregion
            }

            else if (elemName == "VDC TYPE")
            {

                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().vdcType = buffer[1] == 0 ? "integer" : "real";
                if (buffer[1] == '1' && Cgm_Elements.Last().vdc_idx == 0)
                {
                    Cgm_Elements.Last().page_height =
                    Cgm_Elements.Last().page_width =
                    Cgm_Elements.Last().true_height =
                    Cgm_Elements.Last().true_height = 1f;
                }
                #endregion
            }


            else if (elemName == "REAL PRECISION")
            {

                #region MyRegion
                int precision = 0;

                int bcnt = Cgm_Elements.Last().integer_precision / 8;

                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);

                Cgm_Elements.Last().realType = buffer.Last() == 1 ? "fixed" : "floating";

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                precision = buffer.Last();

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                precision += buffer.Last();
                Cgm_Elements.Last().real_precision = precision;
                #endregion

            }
            else if (elemName == "VDC REAL PRECISION")
            {

                #region MyRegion
                int precision = 0;
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);

                Cgm_Elements.Last().vdc_realType = buffer.Last() == 1 ? "fixed" : "floating";

                int bcnt = Cgm_Elements.Last().integer_precision / 8;
                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                precision = buffer.Last();

                buffer = new byte[bcnt];
                br.Read(buffer, 0, buffer.Length);
                precision += buffer.Last();
                Cgm_Elements.Last().vdc_real_precision = precision;
                #endregion


            }
            else if (elemName == "VDC INTEGER PRECISION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().vdc_integer_precision = buffer.Last();
                #endregion

            }
            else if (elemName == "BEGIN APPLICATION STRUCTURE")
            {
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
            }
            else if (elemName == "BEGIN APPLICATION STRUCTURE BODY")
            {
                buffer = new byte[paramLen];
            }
            else if (elemName == "INTEGER PRECISION")
            {

                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().integer_precision = buffer.Last();
                #endregion

            }
            else if (elemName == "TEXT PRECISION")
            {

                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().text_precision = buffer.Last();
                #endregion

            }
            else if (elemName == "CHARACTER CODING ANNOUNCER")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                
                #endregion
            }
            else if (elemName == "MAXIMUM COLOUR INDEX")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().maximum_colour_index = (int)Math.Max(1, (int)buffer[0]);
                #endregion
            }
            else if (elemName == "CHARACTER SET LIST")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                //int n_elems = paramLen / 4;
                //for (int i = 0; i < n_elems; i++)
                //{
                //    buffer = new byte[2];
                //    br.Read(buffer, 0, buffer.Length);
                //    Int16 opc = (Int16)((buffer[0] << 8) | buffer[1]);

                //    str = "";
                //    switch (opc)
                //    {
                //        case 0:
                //            str = "94-character G-set";
                //            break;
                //        case 1:
                //            str = "96-character G-set";
                //            break;
                //        case 2:
                //            str = "94-character multibyte G-set";
                //            break;
                //        case 3:
                //            str = "96-character multibyte G-set";
                //            break;
                //        case 4:
                //            str = "complete code";
                //            break;
                //    }
                //    Cgm_Elements.Last().character_set_list += str + ",";
                //    Console.WriteLine(str);
                //    br.Read(buffer, 0, 2);
                //    buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                //    str = System.Text.Encoding.Default.GetString(buffer);
                //}
                #endregion
            }

            else if (elemName == "METAFILE ELEMENT LIST")
            {
                #region MyRegion
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);

                Int16 n_elems = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                for (int i = 0; i < n_elems; i++)
                {
                    buffer = new byte[2];
                    br.Read(buffer, 0, buffer.Length);

                    Int16 el_class = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);

                    buffer = new byte[2];
                    br.Read(buffer, 0, buffer.Length);

                    Int16 el_id = buffer.Length == 1 ? buffer[0] : (Int16)((buffer[0] << 8) | buffer[1]);
                    str = "";

                    switch (el_id)
                    {
                        case 0:
                            str = "drawing set";
                            break;
                        case 1:
                            str = "drawing-plus-control set";
                            break;
                        case 2:
                            str = "version-2 set";
                            break;
                        case 3:
                            str = "extended-primitives set";
                            break;
                        case 4:
                            str = "version-2-gksm set";
                            break;
                        case 5:
                            str = "version-3 set";
                            break;
                        case 6:
                            str = "version-4 set";
                            break;
                    }
                    Cgm_Elements.Last().metafile_elements = str;


                }

                #endregion
            }

            else if (paramLen > 0)
            {
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Where(fd => fd >= 32).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer);
            }
          
            if (getNext)
            {
                getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);
            }
            buf = buffer;
        }
       
        public void getNextMetaLongLenth(BinaryReader br, byte[] buffer, out int paramLen)
        {
            buffer = new byte[2];
            br.Read(buffer, 0, buffer.Length);

            byte hbyte = buffer[0];
            byte lbyte = buffer[1];


            hbyte = buffer[0];
            lbyte = buffer[1];

            paramLen_rollover = false;
            paramLen = BitConverter.ToUInt16(buffer.Reverse().ToArray(), 0);
            if (paramLen > 32767)
            {
                paramLen_rollover = true;
                paramLen = paramLen & 0x7FFF;
            }
           

          
        }

        public List<Cgm_Element> getNextMetaElement(ref BinaryReader br, ref byte[] buffer, out int bitsread, ref int paramLen, out byte elemclass, out byte elemId, out string elemName, List<Cgm_Element> Cgm_Elements)
        {

            if (br.BaseStream.Position % 2 != 0)
            {
                buffer = new byte[1];
                bitsread = br.Read(buffer, 0, buffer.Length);
            }

            #region MyRegion
            buffer = new byte[2];
            bitsread = br.Read(buffer, 0, buffer.Length);
            byte hbyte = buffer[0];
            byte lbyte = buffer[1];

            string binStr = new String(Convert.ToString(hbyte, 2).ToCharArray().ToArray()).PadLeft(8, '0') +
            new String(Convert.ToString(lbyte, 2).ToCharArray().ToArray()).PadLeft(8, '0');

            UInt16 cw = Convert.ToUInt16(binStr, 2);
            paramLen = (UInt16)(cw & 0x1f);
            
            

            byte elemclassX = (byte)(cw >> 12);
            byte elemIdX = (byte)((cw >> 5) & 0x7f);
            elemId = elemIdX;
            elemclass = elemclassX;
            string[] cgmElemNames = cgm_struct.cgm_element_names.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            cgmElemNames = cgmElemNames.Where(fd => fd.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0].Trim() == elemclassX.ToString()).ToArray();
            cgmElemNames = cgmElemNames.Where(fd => fd.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[1].Trim() == elemIdX.ToString()).ToArray();
            elemName = "";
            if (cgmElemNames != null)
            {
                if (cgmElemNames.Count() == 1)
                {
                    elemName = cgmElemNames[0].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[2].Trim();
                }
                if (elemName == "")
                {
                    Console.WriteLine("err");
                }
            } 
            #endregion
            Cgm_Element lastElem = Cgm_Elements.Count == 0 ? new Cgm_Element() : Cgm_Elements.Last();
            Cgm_Elements.Add(new Cgm_Element
            {
                elem_Class = elemclass.ToString(),
                elem_Id = elemId.ToString(),
                elem_Name = elemName,
                maximum_colour_index = lastElem.maximum_colour_index,
                lineJoin = lastElem.lineJoin,
                lineCap = lastElem.lineCap,
                lineTypeContinue = lastElem.lineTypeContinue,
                dashCapIndicator = lastElem.dashCapIndicator,
                param_length = paramLen,
                long_form_list = paramLen >= 31,
                edgeColor = lastElem.edgeColor,
                strokeColor = lastElem.strokeColor,
                fillColor = lastElem.fillColor,
                edgeWidth = lastElem.edgeWidth,
                edgeType = lastElem.edgeType,
                lineType = lastElem.lineType,
                edgeWidthSet = lastElem.edgeWidthSet,
                lineWidthSet = lastElem.lineWidthSet,
                strokeWidth = lastElem.strokeWidth,
                edgeVisibility = lastElem.edgeVisibility,
                characterHeight = lastElem.characterHeight,
                characterOrientation = lastElem.characterOrientation,
                characterColor = lastElem.characterColor,
                page_width = lastElem.page_width,
                page_height = lastElem.page_height,
                true_width = lastElem.true_width,
                true_height = lastElem.true_height,                
                colourTable_start_idx = lastElem.colourTable_start_idx,
                bgColor = lastElem.bgColor,
                mitreLimit = lastElem.mitreLimit,
                fill_style = lastElem.fill_style,
                hatch_style = lastElem.hatch_style,
                hatch_id =  lastElem.hatch_id,
                pattern_idx = lastElem.pattern_idx,
                vdcType = lastElem.vdcType,
                vdc_integer_precision = lastElem.vdc_integer_precision,
                integer_precision = lastElem.integer_precision,
                vdc_real_precision = lastElem.vdc_real_precision,
                real_precision = lastElem.real_precision,
                colour_precision = lastElem.colour_precision,
                pixel_precision = lastElem.pixel_precision,
                colour_idx_precision = lastElem.colour_idx_precision,
                idx_precision = lastElem.idx_precision,
                colourSelectionMode = lastElem.colourSelectionMode,
                colorModel = lastElem.colorModel,
                fontlist = lastElem.fontlist,
                fontlist_LIST = lastElem.fontlist_LIST,
                fontfamily = lastElem.fontfamily,
                colorTable = lastElem.colorTable,
                isFig = lastElem.isFig,
                scaleFactor = lastElem.scaleFactor,
                isleftRight = lastElem.isleftRight,
                realType = lastElem.realType,
                vdc_realType = lastElem.vdc_realType,
                isbottomUp = lastElem.isbottomUp,
                picture_idx = lastElem.picture_idx,                
                vdc_idx = lastElem.vdc_idx,
                v_alignment_name = lastElem.v_alignment_name,
                h_alignment_name = lastElem.h_alignment_name,
                v_alignment = lastElem.v_alignment,
                h_alignment = lastElem.h_alignment,
                edgeSizeMode = lastElem.edgeSizeMode,
                lineSizeMode = lastElem.lineSizeMode,
                markerSizeMode = lastElem.markerSizeMode 
            });

            //Cgm_Elements.Last().elemParams = new byte[paramLen];
            //br.Read(Cgm_Elements.Last().elemParams, 0, paramLen);
            //br.BaseStream.Seek(-paramLen, SeekOrigin.Current);

            return Cgm_Elements;

        }

        public void createPatter(string pattern_id, string hatch_idx, XmlDocument cgm_svg, string fillColour, float scale)
        {

            XmlNode hatchpattern = cgm_svg.DocumentElement.SelectSingleNode("//pattern[@id='" + pattern_id + "']");
            if (hatchpattern != null)
            {
                return;
            }
            XmlNode defs = cgm_svg.DocumentElement.SelectSingleNode("//defs");


            XmlDocument hatch = new XmlDocument();
            
            hatch_idx =  String.Format("hashtype_{0}", hatch_idx);

            byte[] data = (byte[])cgm_struct.ResourceManager.GetObject(hatch_idx);

            hatch.LoadXml(System.Text.Encoding.Default.GetString(data));

            XmlNamespaceManager xml = new XmlNamespaceManager(hatch.NameTable);
            xml.AddNamespace("space", "http://www.w3.org/2000/svg");
            XmlNode pattern = null;

            pattern = hatch.DocumentElement.SelectSingleNode("//space:path", xml);
            if (pattern == null)
            {
                pattern = hatch.DocumentElement.SelectSingleNode("//space:polygon", xml);
            }

            hatchpattern = cgm_svg.CreateElement("pattern");

            hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("id")).Value = pattern_id;
            hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("patternUnits")).Value = "userSpaceOnUse";
            hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = (40 * scale).ToString();
            hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = (40 * scale).ToString();
            pattern.Attributes.Append(hatch.CreateAttribute("transform")).Value = String.Format("scale({0})", scale);
            pattern.Attributes.Append(hatch.CreateAttribute("fill")).Value = fillColour;
            pattern.Attributes.Append(hatch.CreateAttribute("style")).Value = String.Format("fill:{0};", fillColour);

            hatchpattern.InnerXml = pattern.OuterXml.Replace("xmlns=\"http://www.w3.org/2000/svg\"", "");

            defs.AppendChild(hatchpattern);

        }

        public void createImagePattern(XmlDocument cgm_svg, List<Cgm_Element> PattenTables,float scale)
        {

            foreach (Cgm_Element patten in PattenTables.Where(fd => fd.elem_Name == "PATTERN TABLE").ToList())
            {
                string pattern_id = String.Format("img_patten_{0}", patten.pattern_idx);
                XmlNode hatchpattern = cgm_svg.DocumentElement.SelectSingleNode("//pattern[@id='" + pattern_id + "']");
                if (hatchpattern != null)
                {
                    continue;
                }
                XmlNode defs = cgm_svg.DocumentElement.SelectSingleNode("//defs");
                if (defs == null)
                {
                    cgm_svg.DocumentElement.AppendChild(cgm_svg.CreateElement("defs"));
                }
                hatchpattern = cgm_svg.CreateElement("pattern");

                
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("id")).Value = pattern_id;
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("patternUnits")).Value = "userSpaceOnUse";
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = (patten.rasterImage.Width * scale).ToString();
                hatchpattern.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = (patten.rasterImage.Height * scale).ToString();

                XmlNode imgpattern = cgm_svg.CreateElement("image");
                imgpattern.Attributes.Append(cgm_svg.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink")).Value = string.Format("data:image/bmp;base64,{0}", patten.raster2base64());
                imgpattern.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = (patten.rasterImage.Width).ToString();
                imgpattern.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = (patten.rasterImage.Height).ToString();
                imgpattern.Attributes.Append(cgm_svg.CreateAttribute("transform")).Value = String.Format("scale({0})", scale);

                hatchpattern.AppendChild(imgpattern);
                defs.AppendChild(hatchpattern);

            }
            

        }
        
        public double distance(PointF p1, PointF p2, out double angle)
        {
            double dist =Math.Sqrt( Math.Pow(p2.X - p1.X, 2)  + Math.Pow(p2.Y - p1.Y, 2) );
            angle = Math.Atan2((p2.Y - p1.Y) , ( p2.X - p1.X) ) *  180 / Math.PI;
            
            return dist;
        }
        
        public PointF midPoint(PointF p1, PointF p2)
        {
            return new PointF(
            (p2.X + p1.X) / 2,
            (p2.Y + p1.Y) / 2
            );
            
        }

        public double distance_180(PointF p1, PointF p2)
        {
            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));            
            return dist;
        }
        
        public double distance_180(PointF p1, PointF p2, out double angle, out float slope, out float b_int)
        {
            
            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));            
            angle = Math.Atan2((p2.Y - p1.Y),  (p2.X - p1.X)) * 180 / Math.PI;
            angle = Math.Round(angle, 2);
            int sign = Math.Sign(angle);
            slope =   (float)(( Math.Round(  p2.Y, 2) - Math.Round(  p1.Y, 2)) /( Math.Round(  p2.X, 2) - Math.Round(  p1.X, 2)));
            b_int = p1.X;

            float m1 = p1.Y / p1.X;
            float m2 = p2.Y / p2.X;
            double angleX = Math.Atan2(m1 - m2, (1 + (m1 * m2))) * 180 / Math.PI;

            return dist;
        }

        public double distance_180_xrs(PointF p1, PointF p2, out double angle, out double angle_crx, out float slope, out float b_int)
        {

            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            angle = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;
            int sign = Math.Sign(angle);
            slope = ((p2.Y - p1.Y) / (p2.X - p1.X));
            b_int = p1.X;

            float m1 = p1.Y / p1.X;
            float m2 = p2.Y / p2.X;
            
            if (float.IsNegativeInfinity(m1) && m2 == 0)
            {
                angle_crx = Math.Sign(p2.X)  * -90;
                
            }
            else if (float.IsInfinity(m1) && m2 == 0)
            {
                angle_crx =  Math.Sign(p2.X) * 90;

            }
            else if (float.IsNegativeInfinity(m2) && m1 == 0)
            {
                angle_crx = Math.Sign(p1.X) * 90;
               
            }
            else if (float.IsInfinity(m2) && m1 == 0)
            {
                angle_crx = Math.Sign(p1.X) * -90;
               
            }
  
            else if (m1 == 0 || m2 == 0)
            {
                double v1 = Math.Atan2(p1.Y, p1.X) * 180 / Math.PI;
                double v2 = Math.Atan2(p2.Y, p2.X) * 180 / Math.PI;
    
                if (v2 < v1 && v1 <= 180 + v2)
                {
                    angle_crx = v1 - v2;
                }
                else
                {
                    angle_crx = (360 + v1) - v2;
                    if (angle_crx > 180)
                    {
                        angle_crx = angle_crx - 360;
                    }
                }
               
                
            }
            else
            {
                double v1 = Math.Atan2(p1.Y, p1.X) * 180 / Math.PI;
                double v2 = Math.Atan2(p2.Y, p2.X) * 180 / Math.PI;
                v1 = Math.Round(v1, 3);
                v2 = Math.Round(v2, 3);
                if ( v2 < v1 && v1 <= 180 + v2)
                {
                     angle_crx =  v1- v2;
                }
                else if (v2 < (360 + v1) && (360 + v1) <= 180 + v2)
                {
                    angle_crx = (360 + v1) - v2;
                }
                else if (180 + v2 <  v1 && v1 > 0 )
                {
                    angle_crx = v1 - v2;
                    if (angle_crx > 180)
                    {
                        angle_crx = (angle_crx - 360);
                    }
                }
                else
                {
                    angle_crx = (360 + v1) - v2 - 360;
                }
 
            }
            angle_crx = Math.Round(angle_crx, 3);
            return dist;
        }
        
        public double distance_90(PointF p1, PointF p2, out double angle, out float slope, out float b_int)
        {
            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            angle = Math.Atan((p1.Y - p2.Y) / (p2.X - p1.X)) * 180 / Math.PI;
            slope = (p2.Y - p1.Y) / (p2.X - p1.X);
            b_int = p1.X;
            return dist;
        }

        void getellipse(Cgm_Element cgmElement, out double rx, out double ry, out double angle, out double angle_2)
        {
            double angle_p1 = 0;
            double angle_p2 = 0;
            double angle_1 = 0;
            float slope_D1;
            float slope_D2;
            float b_1;
            float b_2;

            rx = distance_180(cgmElement.points[0], cgmElement.points[1], out angle_p1, out slope_D1, out b_1);
            ry = distance_180(cgmElement.points[0], cgmElement.points[2], out angle_p2, out slope_D2, out b_2);
            angle_2 = (angle_p2 - angle_p1);

            //PointF p1 = PointF.Subtract(cgmElement.points[1], new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
            //PointF p2 = PointF.Subtract(cgmElement.points[2], new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
            //distance_180_xrs(p1, p2, out angle_1, out angle_2, out slope_D2, out b_2);

            
            PointF rP_minor = cgmElement.points[2];
            PointF rP_major = cgmElement.points[1];
            angle_2 = Math.Round(angle_2, 0);
            if ((int)(Math.Cos(Math.Abs(angle_2 * Math.PI / 180)) * 100) != 0 && (int)(Math.Cos(Math.Abs(angle_2 * Math.PI / 180)) * 100) != 100)  
            {
                if (rx > ry)
                {
                }
                else
                {
                    rx = distance_180(cgmElement.points[0], cgmElement.points[2], out angle_p1, out slope_D1, out b_1);
                    ry = distance_180(cgmElement.points[0], cgmElement.points[1], out angle_p2, out slope_D2, out b_2);
                    rP_minor = cgmElement.points[1];
                    rP_major = cgmElement.points[2];
                }            
                slope_D1 = -1 / slope_D1;
                PointF D = new PointF(0, cgmElement.points[0].Y);
                if (float.IsInfinity(slope_D1))
                {
                    D.X = rP_minor.X;
                    D.Y = (float)(rx + rP_minor.Y);
                }
                else
                {
                    double tetha = Math.PI * ((360 + angle_p1) - 90) / 180;
                    double xDelta =(Math.Cos(tetha) * rx);
                    double yDelta =(Math.Sin(tetha) * rx);
              

                    D.X = (float)(rP_minor.X + xDelta);
                    D.Y = (float)(rP_minor.Y + yDelta);
                }
                
                

                double DC_diam = distance_180(cgmElement.points[0], D, out angle_p2, out slope_D2, out b_2);
                PointF C = midPoint(cgmElement.points[0], D);
                double RC = distance_180(rP_minor, C, out angle_p1, out slope_D2, out b_2);

                SizeF jj = new SizeF(
                   Math.Abs ( ((float)(Math.Cos(Math.Atan(slope_D2)) * DC_diam / 2)) )
                    ,
                   Math.Abs(((float)(Math.Sin(Math.Atan(slope_D2)) * DC_diam / 2)))
                    );
                bool sign_m = Math.Sign(slope_D2) == 1;
                bool sign_a = Math.Sign(angle_p1) == 1;

                PointF  G = new PointF();
                PointF E = new PointF();
                 
                if (sign_m && sign_a)
                {
                    G.X = C.X + jj.Width;
                    G.Y = C.Y + jj.Height;
                    E.X = C.X - jj.Width;
                    E.Y = C.Y - jj.Height;                    
                }
                else if (!sign_m && !sign_a)
                {
                    G.X = C.X + jj.Width;
                    G.Y = C.Y - jj.Height;
                    E.X = C.X - jj.Width;
                    E.Y = C.Y + jj.Height;
                }   
                else if (!sign_m && sign_a)
                {
                    G.X = C.X - jj.Width;
                    G.Y = C.Y + jj.Height;
                    E.X = C.X + jj.Width;
                    E.Y = C.Y - jj.Height;
                }
                else if (sign_m && !sign_a)
                {
                    G.X = C.X - jj.Width;
                    G.Y = C.Y - jj.Height;
                    E.X = C.X + jj.Width;
                    E.Y = C.Y + jj.Height;
                }

                rx = distance_180(rP_minor, G, out angle_p2, out slope_D2, out b_2);
                ry = distance_180(rP_minor, E, out angle_p1, out slope_D2, out b_2);

                double dist_GC = distance_180(cgmElement.points[0], G, out angle_p1, out slope_D2, out b_2);

                double dist_EC = distance_180(cgmElement.points[0], E, out angle_p2, out slope_D2, out b_2);
                double dd = angle_p1 - angle_p2;
                angle = angle_p2;
                angle_2 = dd;


                //dist_GC = distance_180_xrs(cgmElement.points[0], G, out angle_p1, out angle_2, out slope_D2, out b_2);

                //dist_EC = distance_180_xrs(cgmElement.points[0], E, out angle_p2, out angle_2, out slope_D2, out b_2);
                //E = PointF.Subtract(E, new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
                //G = PointF.Subtract(G, new SizeF(cgmElement.points[0].X, cgmElement.points[0].Y));
                //distance_180_xrs(G, E, out angle, out angle_2, out slope_D2, out b_2);
                
            }
            else{
                if (rx < ry)
                {
                    angle = rx;
                    rx = ry;
                    ry = angle;
                    angle = angle_p2;
                    angle_2 = angle_2;
                }
                else
                {
                    angle = angle_p1;
                    angle_2 = angle_2;
                }                
            }
            if (angle_2 > 180)
            {
                angle_2 = angle_2 - 360;
            }
        }
        
        void getellipse(Cgm_Element cgmElement, out double rx, out double ry, out double angle)
        {
            double angle_p1 = 0;
            double angle_p2 = 0;                        
            float slope_D1;
            float slope_D2;
            float b_1;
            float b_2;

            
            rx = distance_90(cgmElement.points[0], cgmElement.points[1], out angle_p1, out slope_D1, out b_1);
            ry = distance_90(cgmElement.points[0], cgmElement.points[2], out angle_p2, out slope_D2, out b_2);
            angle = (angle_p2 - angle_p1);

            if ( (int)Math.Sin(angle) != 0  )
            {
                SizeF ss = (new SizeF(cgmElement.points[1]) - new SizeF(cgmElement.points[0]));
                ss.Height = Math.Abs(ss.Height);
                ss.Width = Math.Abs(ss.Width);
                slope_D1 = -1 / slope_D1;
                PointF x_axis = new PointF(0, cgmElement.points[0].Y);
                x_axis.X = (cgmElement.points[0].Y - cgmElement.points[1].Y + slope_D1 * cgmElement.points[0].X) / slope_D1;

                PointF D = x_axis;

                double DC_diam = distance_90(cgmElement.points[0], D, out angle_p2, out slope_D2, out b_2);
                PointF C = midPoint(cgmElement.points[0], D);
                double RC = distance_90(C, cgmElement.points[2], out angle_p1, out slope_D2, out b_2);

                SizeF jj = new SizeF(
                    ((float)(Math.Cos(Math.Atan(slope_D2)) * DC_diam / 2))
                    ,
                   ((float)(Math.Sin(Math.Atan(slope_D2)) * DC_diam / 2))
                    );
                PointF G = PointF.Subtract(C, jj);
                PointF E = PointF.Add(C, jj);
                rx = distance_90(cgmElement.points[2], G, out angle_p2, out slope_D2, out b_2);
                ry = distance_90(cgmElement.points[2], E, out angle_p2, out slope_D2, out b_2);
                double dist_EC = distance_90(cgmElement.points[0], E, out angle, out slope_D2, out b_2);
                
            }
            else
            {
                angle = angle_p1;
            }

        }

        public PointF finfPontOnElispe(float angle, float d1, float d2, float h, float k, float m, float x1, float y1, float xMax)
        {
            if (d2 == 0)
            {
                return new PointF(h, k);
            }
            float c_teta = (float)((Math.Cos(angle * Math.PI / 180)));
            float s_teta = (float)((Math.Sin(angle * Math.PI / 180)));

            float c_teta_2 = c_teta * c_teta;
            float s_teta_2 = s_teta * s_teta;
            float a2 = d1 * d1;
            float b2 = d2 * d2;
            
            float A, B, C, D, E, F;

            A = (c_teta_2 / a2) + (s_teta_2 / b2);
            B = 2 * (c_teta * s_teta * ((1 / a2) - (1 / b2)));            
            C = (s_teta_2 / a2 )+ (c_teta_2 / b2);

            float x = 0;
            float y = 0;
            float res = 1;

            float h2 = h*h;
            float k2 = k*k;
            float xr0 = Math.Min(xMax, h) - 1;
            float xr1 = Math.Max(xMax, h);
            float[] range = { 0 };
            float d3 = Math.Max(d2, d1);
            #region MyRegion
           
            if (m == 0)
            {

                if (h > xMax)
                {
                    xr1 = h ;
                    xr0 = h - (d3 + 1);
                    xr0 = Math.Min(xMax, xr0) - 2;
                }
                else if (h < xMax)
                {
                    xr0 = h ;
                    xr1 = h + (d3 + 1);
                    xr1 = Math.Max(xMax, xr1);
                }
            }
            else if (float.IsInfinity(m))
            {
                if (float.IsNegativeInfinity(m))
                {
                    xr1 = k;
                    xr0 = k - (2*d3);
                    
                }
                else
                {
                    xr1 = Math.Max(xMax, k);

                    if (k > xMax)
                    {
                        xr1 = k - 1;
                        xr0 = k - (d3 + 1);
                        xr0 = Math.Min(xr0, xMax);
                    }
                    else
                    {
                        xr0 = k + 1;
                        xr1 = k + (d3 + 1);
                        xr1 = Math.Max(xMax, xr1) + 1;
                    }
                }
            }
            else
            {
                if (h > xMax)
                {
                    xr1 = h + 3;
                    xr0 = h - (d3 + 3);
                    xr0 = Math.Min(xMax, xr0) - 2;
                }
                else if (h < xMax)
                {
                    xr0 = h;
                    xr1 = h + (d3 + 3);
                    xr1 = Math.Max(xMax, xr1);
                }
            }
            float upperLimit = (xr1 - xr0 + 1) * 1;
            //range = Enumerable.Range((int)xr0-1, (int)(upperLimit)/0.01).Select(fd => (float)(fd + (1 / 1))).ToArray();
            range = Enumerable.Range(0, (int)(upperLimit/0.010)).Select(fd => (float)(xr0 - 0) + ((float)fd / 100)).ToArray();
            
                
            int i = 0;
            int sign = 0;


            if (float.IsInfinity(m))
            {
                x = h;
                y = range[i];
            }
            else if (m == 0)
            {
                x = range[i];
                y = k;
            }
            else
            {
                x = range[i];
                y = m * (x - x1) + y1;
            }


            float A1 = A * x * x;
            float B1 = B * x * y;
            float C1 = C * y * y;

            D = ((2 * A * h) + (k * B)) * x;
            E = ((2 * C * k) + (B * h)) * y;

            F = (A * h2) + (B * h * k) + (C * k2) - 1;

            res = A1 + B1 + C1 - D - E + F;
            sign = Math.Sign(res);
            i++;


            PointF intercection = new PointF();
            int max_i = range.Length;
            bool solved = false;
            y = 0;
            //Console.WriteLine(res.ToString() + "\t" + x + "\t" + y); 
            #endregion
            
            for (; i < max_i; i++)
            {
                //Console.WriteLine(res.ToString() + "\t" + x + "\t" + y);
                if (float.IsInfinity(m))
                {
                    x = h;
                    y = range[i];
                }
                else
                {
                    x = range[i];                   
                    y = m * (x - x1) + y1;
                }

                A1 = A * x * x;
                B1 = B * x * y;
                C1 = C * y * y;                
                D = ((2 * A * h) + (k * B)) * x;
                E = ((2 * C * k) + (B * h)) * y;
                F = (A * h2) + (B * h * k) + (C * k2) - 1;
                
                res = A1 + B1 + C1 - D - E + F;
                //Console.WriteLine(res.ToString() + "\t" + x + "\t" + y);
                if (sign != Math.Sign(res) )
                {
                    if (!float.IsInfinity(m))
                    {
                        if (i > 1)
                        {
                            intercection.X = x - (Math.Abs(x - intercection.X) / 2);
                            if (m > 0)
                            {
                                intercection.Y = y - (Math.Abs(y - intercection.Y) / 2);
                            }
                            else if (m < 0)
                            {
                                intercection.Y = y + (Math.Abs(y - intercection.Y) / 2);
                            }
                        }
                        else
                        {
                            intercection.X = x;
                            intercection.Y = y;
                        }
                    }
                    else
                    {
                        intercection.X = x;
                        intercection.Y = y;
                    }
                    solved = true;
                    break;                    
                }

                intercection.X = x;
                intercection.Y = y;
            }
            if (solved == false)
            {
                Console.WriteLine("no solution found");
            }
            return intercection;
        }

        public PointF solve2Pt_arc_c(PointF a, PointF b, PointF c, int xMax)
        {
            
            double a1, a2;

            float m1, m2,  b1;

            double d1 = distance_180(a, b, out a1 ,out m1,out b1);

            double d2 = distance_180(b, c, out a2,out m2,out b1);

            PointF D = midPoint(a, b);
            PointF E = midPoint(b, c);

            m1 = -1 / m1;
            m2 = -1 / m2;
            float x1 = b.X;
            float y1 = b.Y;
            int accur = 1;
            float[] domain = Enumerable.Range(0, xMax * accur).Select(fd => (float)(fd / accur)).ToArray();
            int s0 = 0;
            int aa = 0;
            float bb = 0;
            PointF intecection = new PointF();
            foreach( float x in domain)
            {


                float ya = m1 * (x - D.X) + D.Y;
                float yb =  m2*(x - E.X) + E.Y;
                
                //Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", x, ya, yb, (ya - yb)));
                if(x > 0)
                {
                    if( s0 !=  Math.Sign(ya - yb))
                    {
                        //Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", x, ya, yb, (ya - yb)));
                        //Console.WriteLine(bb - (ya - yb));
                        //intecection.X = x + Math.Abs(x - intecection.X) / 2;
                        //intecection.Y = ya;
                        break;
                    }
                }
                intecection.X = (int)x;
                intecection.Y = (int)ya;
            
                bb = ya - yb;
            }
            return intecection;
        }

        public void getArcParams_ell(float angle_dir,  float angle_LEN, out char path_len, out char dir)
        {

            path_len = '0';
            dir = '0';
            bool flipped = false;
            bool flipped2 = false;
            if (angle_dir < 0)
            {
                angle_dir = 360 + angle_dir;
                flipped = true;
            }

            //angle_dir = (float)Math.Round(angle_dir, 0);

            if (angle_LEN < 0)
            {
                flipped2 = true;
                angle_LEN = 360 + angle_LEN;                
            }
            if (flipped == false)
            {

                if (angle_dir > 180 && angle_dir < 270)
                {
                    path_len = '0';
                    dir = '0';
                }
                else if (angle_dir > 90 && angle_dir < 180)
                {
                    path_len = angle_LEN < 180 ? '0' : '1';
                    dir = '1';   
                }
                else if (angle_dir > 0 && angle_dir < 90)
                {
                    path_len = angle_LEN < 180 ? '0' : '1';
                    dir = '1';                    
                }
                else if (angle_dir > 270 && angle_dir < 360)
                {
                    path_len = '0';
                    if (360 - angle_LEN > 180)
                    {
                        path_len = '1';
                    }
                    dir = '0';
                }

                else if (angle_dir == 90)
                {
                    if (!flipped2)
                    {
                        path_len = angle_LEN < 180 ? '0' : '1';
                        dir = '1';
                    }
                    else
                    {
                        path_len = angle_LEN > 180 ? '1' : '0';
                        dir = '1';
                    }
                }
                else if (angle_dir == 270)
                {
                    path_len = '0';
                    dir = '0';
                }
                else if (angle_dir == 180)
                {
                    path_len = '1';
                    dir = '0';
                }        
                else if (angle_dir == 360)
                {
                    path_len = '0';
                    dir = '0';
                }
            }
            else if (flipped)
            {

                if (angle_dir > 180 && angle_dir < 270)
                {
                    path_len = angle_LEN > 180 ? '0' : '1';
                    dir = '0';
                }
                else if (angle_dir > 90 && angle_dir < 180)
                {
                    path_len = '0';
                    dir = '1';
                }
                else if (angle_dir > 0 && angle_dir < 90)
                {
                    path_len = '0';
                    dir = '1';
                }
                else if (angle_dir > 270 && angle_dir < 360)
                {
                    path_len = angle_LEN < 180 ? '1' : '0';
                    dir = '0';
                }
                else if (angle_dir == 270) 
                {               
                        dir = '0';
                        path_len = angle_LEN < 180 ? '1' : '0';                                        
                }
                else if (angle_dir == 180) 
                {
                    dir = '0';
                }
                else if (angle_dir == 90)
                {
                    dir = '1';
                    
                }
                else if (angle_dir == 360) 
                {
                    dir = '0';
                }
                

            }
        }
        
        public void getArcParams_ell_xrs(float angle_dir, float angle_dir2, float angle_dir3, out char path_len, out char dir) 
        {
            float alen = angle_dir;
            if (angle_dir2 > 0)
            {
                if (angle_dir2 < 180)
                {
                    if (alen < 0)
                    {
                        if (Math.Abs(angle_dir) < 180 & Math.Abs(angle_dir3) < 180  && (angle_dir3 < 0))
                        {
                            path_len = '0';
                            dir= '0';
                        }
                        else
                        {
                            alen = 360 + alen;
                            if (angle_dir3 < 0)
                            {
                                path_len = alen < 180 ? '0' : '1';
                                dir = angle_dir < 0 ? '1' : '0';
                            }
                            else
                            {
                                path_len = alen < 180 ? '0' : '1';
                                dir = angle_dir < 0 ? '1' : '0';
                            }
                        }
                    }
                    else
                    {
                        if (angle_dir3 > 180)
                        {
                            path_len = '1';
                            dir = angle_dir < 0 ? '1' : '0';
                        }
                        else
                        {
                            if (angle_dir3 < 0)
                            {
                                path_len = alen < 180 ? '1' : '0';
                                dir = angle_dir3 < 0 ? '0' : '1';
                            }
                            else
                            {
                                path_len = alen < 180 ? '0' : '1';
                                dir = angle_dir < 0 ? '0' : '1';
                            }
                            
                        }

                    }
                }
                else
                {
                    //if (alen < 0)
                    {
                        alen = 360 + alen;
                        path_len = alen < 180 ? '0' : '1';

                        dir = angle_dir < 0 ? '1' : '0';
                    }
                }
            }
            else
            {
                if (alen < 0)
                {

                    if (angle_dir3 > 0)
                    {
                        alen = 360 + alen;
                        path_len = angle_dir3 < 180 ? '1' : '0';
                        dir = angle_dir <= 180 ? '1' : '0';
                    }
                    else
                    {
                        alen = 360 + alen;
                        path_len = alen < 180 ? '1' : '0';
                        dir = angle_dir < 0 ? '0' : '1';
                    }
                }
                else
                {
                    
                    if (angle_dir3 > 0)
                    {
                        path_len = angle_dir3 < 180 ? '0' : '1';
                        dir = angle_dir <= 180 ? '1' : '0';
                    }
                    else if (angle_dir3 < -180)
                    {
                        path_len = alen <= 180 ? '0' : '1';
                        dir = angle_dir < 0 ? '1' : '0';
                    }
                    else
                    {
                        path_len = alen <= 180 ? '1' : '0';
                        dir = angle_dir < 0 ? '1' : '0';
                    }
                    

                }
            }
            
            
        }
        
        public void getArcParams_cir(float angle_dir, out char path_len, out char dir )
        {
            path_len = '0';
            dir = '0';
            bool flipped = false;
            if (angle_dir < 0)
            {
                angle_dir = 360 + angle_dir;
                flipped = true;
            }
            angle_dir = (float)Math.Round(angle_dir, 3);
            if (flipped == false)
            {
                if(angle_dir == 0)
                {
                    path_len = '1';
                    dir = '0';
                }
                else if (angle_dir > 180 && angle_dir <= 270)
                {
                    path_len = '0';
                    dir = '0';
                }
                else if (angle_dir > 90 && angle_dir <= 180)
                {
                    path_len = '1';
                    dir = '0';
                }
                else if (angle_dir > 0 && angle_dir < 90)
                {
                    path_len = '0';
                    dir = '1';
                }
                else if (angle_dir > 270 && angle_dir <= 360)
                {
                    path_len = '0';
                    dir = '0';
                }
                else if (angle_dir  == 90 )
                {
                    path_len = '0';
                    dir = '1';
                }
            }
            else if (flipped)
            {

                if (angle_dir > 180 && angle_dir <= 270)
                {
                    path_len = '0';
                    dir = '0';
                }
                else if (angle_dir >= 90 && angle_dir <= 180)
                {
                    path_len = '1';
                    dir = '0';
                }
                else if (angle_dir > 0 && angle_dir < 90)
                {
                    path_len = '0';
                    dir = '1';
                }
                else if (angle_dir > 270 && angle_dir <= 360)
                {
                    path_len = '0';
                    dir = '0';
                }
             
            }
        }
        
    }
}
