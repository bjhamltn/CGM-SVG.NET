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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;


#region JCGM: Used for testing and troble shooting and is not required by the converter.
using net.sf.jcgm.core;  
#endregion

namespace cgm_decoder
{



    public partial class Form1 : Form
    {

        #region debug enable console write line of decoded metafile name
        string filename = "celary03";
        int debug = 0;
        bool altSet = 1 == 0; 
        #endregion

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
            public string colour_value_extent;
            public string elem_Class;
            public string elem_Id;
            public string vdcType;
            public string realType;
            public string vdc_realType;
            public string elem_Name;
            public byte[] elemParams;
            public int param_length;
            public bool long_form_list;
            public bool edgeVisibility;
            public List<PointF> points;
            public List<string> polygonSetFlags;
            public PointF position;
            public String text;
            public Color bgColor;
            public Color strokeColor;
            public Color fillColor;
            public Color edgeColor;
            public Color characterColor;
            public float strokeWidth;
            public float edgeWidth;
            public string edgeType;
            public string lineType;
            public string scalingMode;
            public float characterHeight;
            public float width;
            public float height;
            public string fontlist;
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
            public string lineJoin;
            public string lineCap;
            public string lineTypeContinue;
            public string dashCapIndicator;
            public string metafile_elements;
            public float mitreLimit;
            public string colourSelectionMode;
            public string character_set_list;
            public int colour_precision;
            public int colour_idx_precision;
            public Color[] colorTable;
            #endregion


            #region Cgm_Element Methods

            public Cgm_Element()
            {
                isCircle = false;
                mitreLimit = 4;
                lineEdgeDefs = new LineEdgeType();
                end_fig = start_fig = false;
                vdcType = "integer";
                realType = "floating";
                vdc_realType = "floating";
                isFig = false;
                colorModel = "RGB";
                integer_precision = 16;
                vdc_real_precision = 16;
                colour_precision = 24;
                real_precision = 32;
                vdc_integer_precision = 16;
                page_height = 0;
                page_width = 0;
                elem_Class = "";
                elem_Id = "";
                elem_Name = "";
                elemParams = new byte[0];
                points = new List<PointF>();
                polygonSetFlags = new List<string>();
                characterColor = fillColor = strokeColor = edgeColor = Color.FromArgb(255, 0, 0, 0);

                characterHeight = 16;
                edgeWidth = 1f;
                strokeWidth = 1f;
                lineCap = "round";
                lineJoin = "round";
                mitreLimit = 0;
                edgeVisibility = true;
                fill_style = "hollow";


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

                            byte[] fraction = new byte[2];
                            Array.Copy(bytes, 2, fraction, 0, 2);
                            Array.Resize(ref bytes, 2);
                            double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                            return (float)dd;

                        }
                    }
                    else if (realType == "fixed")
                    {
                        if (precision == 32)
                        {
                            byte[] fraction = new byte[2];
                            Array.Copy(bytes, 2, fraction, 0, 2);
                            Array.Resize(ref bytes, 2);
                            double dd = BitConverter.ToInt16(bytes.Reverse().ToArray(), 0) + (BitConverter.ToUInt16(fraction.Reverse().ToArray(), 0)) / Math.Pow(2, 16);
                            return result;
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
                else if (vdc_real_precision == 32)
                {
                    result = BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
                }
                else if (vdc_real_precision == 16)
                {
                    result = BitConverter.ToUInt16(bytes.Reverse().ToArray(), 0);
                }

                return result;
            }

            public float bytes_getValue_int(byte[] bytes, int precision)
            {
                float result = 0;
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
                        //colour_precision = 32;
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
                    else if (colorTable.Length > buffer[0])
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
                    else if (colorTable.Length > colr_idx)
                    {
                        elemColor = colorTable[colr_idx];
                    }
                }

                return elemColor;
            }

            public Color extractColor(byte[] buffer, ref Color elemColor)
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
                    if (colorTable.Length > buffer[0])
                    {
                        elemColor = colorTable[buffer[0]];
                    }
                }
                else if (param_length < 3)
                {
                    colour_precision = 8 * param_length;
                    int colr_idx = (int)bytes_getValue_color(buffer, colour_precision);
                    if (colorTable == null)
                    {
                        elemColor = Color.FromArgb(255, colr_idx, colr_idx, colr_idx);
                    }
                    else if (colorTable.Length > colr_idx)
                    {
                        elemColor = colorTable[colr_idx];
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
                    base64String = Convert.ToBase64String(imageBytes,Base64FormattingOptions.None);
                }
                StreamWriter sw = new StreamWriter("dsfsdfsd.txt", false);
                sw.WriteLine(base64String);
                sw.Close();
                return base64String;
            }

            #endregion
        }

        public List<LineEdgeType> lineEdgeTypeLookUp = new List<LineEdgeType>();
        
        public Form1()
        {
            #region Sets the location of the file being converted and the location of the output file.
            string cgmfilename_in = @"C:\Users\795627\Downloads\Reference Files\ata30-cgms\" + filename + ".cgm";
            string cgmfilename_out = @"C:\Users\795627\Desktop\" + filename + ".txt";
            if (altSet)
            {
                cgmfilename_in = @"C:\Users\795627\Desktop\" + filename + ".cgm";
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
            //java.io.File cgmFile = new java.io.File(cgmfilename_in);
            //java.io.DataInputStream input = new java.io.DataInputStream(new java.io.FileInputStream(cgmFile));
            //net.sf.jcgm.core.CGM cgm = new CGM();
            //cgm.read(input);
            //List<Command> commands = cgm.getCommands().toArray().Cast<Command>().ToList();
            //StreamWriter sw = new StreamWriter(cgmfilename_out, false);
            //foreach (Command cmd in commands)
            //{
            //    sw.WriteLine(cmd.toString());
            //}
            //sw.Close();            
            #endregion

            BinaryReader br = new BinaryReader(new FileStream(cgmfilename_in, FileMode.Open, FileAccess.Read));

            
            int paramLen = 0;
            byte elemclass = 0;
            byte elemId = 0;
            string elemName = "";
            byte[] buffer= new byte[0];
            int bytesread = 0;
            List<Cgm_Element> Cgm_Elements = new List<Cgm_Element>();

            getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);

            while (bytesread > 0)
            {
                if (elemName != "")
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

        public void parseMetaElement(ref BinaryReader br, ref byte[] buf, ref int bytesread, ref int paramLen, ref byte elemclass, ref byte elemId, ref string elemName, ref List<Cgm_Element> Cgm_Elements, bool getNext)
        {
            byte[] buffer = buf;
            string str = "";
            if (debug == 1)
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

                int w = (int)(Math.Abs(pqr[0].X - pqr[1].X));
                int h = (int)(Math.Abs(pqr[0].Y - pqr[1].Y));

                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int nx = (int)Cgm_Elements.Last().bytes_getValue(buffer, precision);


                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                int ny = (int)Cgm_Elements.Last().bytes_getValue(buffer, precision);

                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                float color_precision = Cgm_Elements.Last().bytes_getValue(buffer, precision);


                buffer = new byte[byteLen];
                bytesRead += br.Read(buffer, 0, buffer.Length);
                float row_mode = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                bool indexedColor = Cgm_Elements.Last().colorTable != null;
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
                    buffer = new byte[paramLen - bytesRead];
                    bytesRead += br.Read(buffer, 0, buffer.Length);
                    float pixelSize = buffer.Length / (nx * ny);

                    byte[] pixel = new byte[(int)pixelSize];
                    for (int i = 0, j = 0; i < buffer.Length; i++)
                    {
                        pixel[j++] = buffer[i];
                        if (j % pixelSize == 0 && i > 0)
                        {
                            j = 0;
                            Color elemColor = new Color();
                            pixels.Add(Cgm_Elements.Last().pixelColor(pixel, ref elemColor, (int)pixelSize));
                            pixel = new byte[(int)pixelSize];
                        }

                    }
                    #endregion
                }

                #region Create Bitmap
                Cgm_Elements.Last().rasterImage  = new Bitmap((int)w, (int)h);
                Graphics g = Graphics.FromImage(Cgm_Elements.Last().rasterImage);
                int xrex = w / nx;
                int yres = h / ny;
                int k = 0;
                for (int i = 0; i < ny; i++)
                {
                    for (int j = 0; j < nx; j++)
                    {
                        Color pixel_c = pixels[k];
                        g.FillRectangle(new SolidBrush(pixel_c), j * xrex, i * yres, xrex, yres);
                        k++;
                    }
                }
                

                #endregion

                if (br.BaseStream.Position % 2 != 0)
                {
                    bytesRead += br.Read(buffer, 0, 1);
                }  
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


                    buffer = new byte[2];
                    br.Read(buffer, 0, buffer.Length);
                    string polygonFlag = "";
                    switch (buffer[1])
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
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().polybezier_continuous = buffer[1];
                int precision = Cgm_Elements.Last().getPrecision();
                int byteLen = precision / 8;
                int points_cnt = (paramLen - 2) / (byteLen * 2);
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
                int words_cnt = (paramLen - (byteLen)) / (byteLen * 2);
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
                
                float closure = Cgm_Elements.Last().bytes_getValue(buffer, precision);
                
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
                int words_cnt = ((paramLen - (2 * byteLen)) / (byteLen * 2));
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

                buffer = new byte[byteLen];
                br.Read(buffer, 0, buffer.Length);
                float closure = Cgm_Elements.Last().bytes_getValue(buffer, precision);
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

                #region MyRegion
                int vdc_cnt = 3;
                int precision = Cgm_Elements.Last().getPrecision_vdc();
                int byteLen = precision / 8;
                while (vdc_cnt > 0)
                {
                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);

                    float px = 0;
                    px = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    float py = 0;
                    py = Cgm_Elements.Last().bytes_getValue(buffer, precision);

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

                    px = Cgm_Elements.Last().bytes_getValue(buffer, precision);

                    buffer = new byte[byteLen];
                    br.Read(buffer, 0, buffer.Length);
                    py = Cgm_Elements.Last().bytes_getValue(buffer, precision);


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
                Matrix<float> P = Matrix<float>.Build.DenseOfArray(new float[,] { { .25f  } , {.25f } });

                Matrix<float> PT =(  M * P ) + C;

                float  b, m;
                double a;
                float p_dist = (float)distance_180(pts[0], new PointF(PT[0, 0], PT[1, 0]), out a, out m, out b);
                
                int  sign_x = Math.Sign( Math.Cos( 360 + a ));
                int sign_y = Math.Sign(Math.Sin(360 + a));

                distance_180(pts[0], pts[1], out a, out m, out b);
                a = a * (Math.PI / 180);
                float xx = (float)(p_dist * Math.Cos(a)) + pts[0].X;
                float yy = (float)(p_dist * Math.Sin(a)) + pts[0].Y ;
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
                ii -= br.Read(buffer, 0, 1);

                buffer = new byte[ii];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();
                str = System.Text.Encoding.Default.GetString(buffer).Trim();
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
                        Cgm_Elements.Last().page_width = Math.Max(Cgm_Elements.Last().vdcExtent[1].X , Cgm_Elements.Last().vdcExtent[0].X);
                    }
                    else 
                    {
                        Cgm_Elements.Last().page_width = Math.Abs(Cgm_Elements.Last().vdcExtent[1].X - Cgm_Elements.Last().vdcExtent[0].X);
                    }

                    if ((Cgm_Elements.Last().vdcExtent[1].Y > 0) && (Cgm_Elements.Last().vdcExtent[0].Y > 0))
                    {
                        Cgm_Elements.Last().page_height = Math.Max(Cgm_Elements.Last().vdcExtent[1].Y , Cgm_Elements.Last().vdcExtent[0].Y);
                    }
                    else
                    {
                        Cgm_Elements.Last().page_height = Math.Abs(Cgm_Elements.Last().vdcExtent[1].Y - Cgm_Elements.Last().vdcExtent[0].Y);
                    }

                    
                    
                    Cgm_Elements.Last().true_height = Cgm_Elements.Last().page_height;
                    Cgm_Elements.Last().true_width = Cgm_Elements.Last().page_width;
                    words_cnt = words_cnt - 2;
                }
                #endregion
            }
            else if (elemName == "METAFILE DEFAULTS REPLACEMENT")
            {
                #region MyRegion
                int words_cnt = paramLen / 2;
                while (words_cnt > 0)
                {
                    getNextMetaElement(ref br, ref buffer, out bytesread, ref paramLen, out elemclass, out elemId, out elemName, Cgm_Elements);
                    words_cnt -= bytesread;
                    parseMetaElement(ref br, ref buffer, ref bytesread, ref paramLen, ref elemclass, ref elemId, ref elemName, ref Cgm_Elements, false);
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

            else if (elemName == "INTERIOR STYLE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                string fill_style = "";
                switch (buffer[1])
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
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "TEXT REPRESENTATION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "MARKER REPRESENTATION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "LINE REPRESENTATION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "EDGE REPRESENTATION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                #endregion
            }
            else if (elemName == "LINE AND EDGE TYPE DEFINITION")
            {

                #region MyRegion
                Cgm_Elements.Last().lineEdgeDefs = new LineEdgeType();

                Cgm_Elements.Last().lineEdgeDefs.dashseq = new List<float>();


                int p = Cgm_Elements.Last().integer_precision;
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);

                Int16 id = BitConverter.ToInt16(buffer.Reverse().ToArray(), 0);
                Cgm_Elements.Last().lineEdgeDefs.id = id;

                buffer = new byte[4];
                br.Read(buffer, 0, buffer.Length);
                float len = BitConverter.ToSingle(buffer.Reverse().ToArray(), 0);
                Cgm_Elements.Last().lineEdgeDefs.dashCycle_Length = len;

                int b_len = p / 8;
                int dash_cnt = (paramLen - 6) / b_len;
                while (dash_cnt > 0)
                {
                    buffer = new byte[b_len];
                    br.Read(buffer, 0, buffer.Length);
                    Cgm_Elements.Last().lineEdgeDefs.dashseq.Add((int)Cgm_Elements.Last().bytes_getValue_int(buffer, p));
                    dash_cnt -= 1;
                }
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
            else if (elemName == "MARKER SIZE SPECIFICATION MODE")
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
            else if (elemName == "EDGE WIDTH SPECIFICATION MODE")
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
            else if (elemName == "LINE JOIN")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                int ff = br.Read(buffer, 0, buffer.Length);
                Int16 opc = (Int16)((buffer[0] << 8) | buffer[1]);
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
            else if (elemName == "LINE CAP")
            {
                #region MyRegion
                buffer = new byte[paramLen / 2];
                int ff = br.Read(buffer, 0, buffer.Length);
                Int16 opc = (Int16)((buffer[0] << 8) | buffer[1]);
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
                opc = (Int16)((buffer[0] << 8) | buffer[1]);
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
            else if (elemName == "EDGE TYPE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Int16 edge_t = (Int16)((buffer[0] << 8) | buffer[1]);
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
                Int16 edge_t = (Int16)((buffer[0] << 8) | buffer[1]);
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
                Int16 edge_t = (Int16)((buffer[0] << 8) | buffer[1]);
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
            else if (elemName == "COLOUR MODEL")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().colorModel = buffer[1] > 5 ?
                    (new string[] { "", "RGB", "CIELAB", " CIELUV", "CMYK", "RGB-related" })[buffer[1]] : "reserved for registered values";
                #endregion
            }
            else if (elemName == "COLOUR VALUE EXTENT")
            {
                #region MyRegion
                if (paramLen == 6)
                {
                    Cgm_Elements.Last().colour_value_extent = "RGB/CMYK";
                    buffer = new byte[3];
                    br.Read(buffer, 0, buffer.Length);

                    Color cc = Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);
                    Cgm_Elements.Last().min_rgb = cc;
                    buffer = new byte[3];
                    br.Read(buffer, 0, buffer.Length);
                    cc = Color.FromArgb(255, buffer[0], buffer[1], buffer[2]);

                    Cgm_Elements.Last().max_rgb = cc;
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
                buffer = new byte[1];
                br.Read(buffer, 0, 1);
                buffer = new byte[paramLen - 1];

                br.Read(buffer, 0, buffer.Length);
                int st_idx = buffer[0];
                if (Cgm_Elements.Last().colour_precision == 16)
                {
                    int[] idx_breaks = Enumerable.Range(0, buffer.Length / 6).Select(x => x * 6).ToArray();
                    Color[] sdasda = Cgm_Elements.Last().colorTable = idx_breaks.Select(i => Color.FromArgb(255, buffer[i + 2], buffer[i + 4], buffer[i + 6])).ToArray();
                }
                else if (Cgm_Elements.Last().colour_precision == 8)
                {
                    int[] idx_breaks = Enumerable.Range(0, buffer.Length / 3).Select(x => x * 3).ToArray();
                    if (buffer.Length % 3 == 0)
                    {
                        Color[] sdasda = Cgm_Elements.Last().colorTable = idx_breaks.Select(i => Color.FromArgb(255, buffer[i +0], buffer[i + 1], buffer[i + 2])).ToArray();
                    }
                    else
                    {
                        Color[] sdasda = Cgm_Elements.Last().colorTable = idx_breaks.Select(i => Color.FromArgb(255, buffer[i + 1], buffer[i + 2], buffer[i + 3])).ToArray();
                    }
                    
                }
                else if (Cgm_Elements.Last().colorModel == "RGB")
                {
                    int[] idx_breaks = Enumerable.Range(0, buffer.Length / 3).Select(x => x * 3).ToArray();
                    Color[] sdasda = Cgm_Elements.Last().colorTable = idx_breaks.Select(i => Color.FromArgb(255, buffer[i], buffer[i + 1], buffer[i + 2])).ToArray();
                }
                else if (Cgm_Elements.Last().colorModel == "CMYK")
                {
                    int[] idx_breaks = Enumerable.Range(0, buffer.Length / 4).Select(x => x * 4).ToArray();
                    Color[] sdasda = Cgm_Elements.Last().colorTable = idx_breaks.Select(i => Color.FromArgb(buffer[i + 3], buffer[i], buffer[i + 1], buffer[i + 2])).ToArray();                
                }
                //Cgm_Elements.Last().resetColors_fromColorTable();

                #endregion
            }
            else if (elemName == "COLOUR PRECISION")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().colour_precision = buffer[1];
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
                #endregion
            }


            else if (elemName == "EDGE WIDTH")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                float f = Cgm_Elements.Last().bytes_getValue_edge(buffer);
                Cgm_Elements.Last().edgeWidth = f;
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
            else if (elemName == "FONT LIST")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Select(fd => fd <= 32 ? (byte)' ' : fd).ToArray();

                str = System.Text.Encoding.ASCII.GetString(buffer);
                Cgm_Elements.Last().fontlist = str;
                #endregion
            }
            else if (elemName == "BEGIN METAFILE")
            {
                #region MyRegion
                if (paramLen > 0)
                {
                    buffer = new byte[paramLen - 1];
                    br.Read(buffer, 0, 1);
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
                #endregion
            }


            else if (elemName == "REAL PRECISION")
            {
                #region MyRegion
                int precision = 0;
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);

                Cgm_Elements.Last().realType = buffer.Last() == 1 ? "fixed" : "floating";
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                precision = buffer.Last();

                buffer = new byte[2];
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
                buffer = new byte[2];
                br.Read(buffer, 0, buffer.Length);
                precision = buffer.Last();

                buffer = new byte[2];
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
                Cgm_Elements.Last().vdc_integer_precision = buffer.Last();
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
                Cgm_Elements.Last().maximum_colour_index = buffer[0];
                #endregion
            }
            else if (elemName == "MAXIMUM COLOUR INDEX")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                Cgm_Elements.Last().maximum_colour_index = buffer[0];
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
                Int16 n_elems = (Int16)((buffer[0] << 8) | buffer[1]);
                for (int i = 0; i < n_elems; i++)
                {
                    buffer = new byte[2];
                    br.Read(buffer, 0, buffer.Length);
                    int el_class = (Int16)((buffer[0] << 8) | buffer[1]);
                    buffer = new byte[2];
                    br.Read(buffer, 0, buffer.Length);
                    int el_id = (Int16)((buffer[0] << 8) | buffer[1]);
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



            else if (elemName == "COLOUR SELECTION MODE")
            {
                #region MyRegion
                buffer = new byte[paramLen];
                br.Read(buffer, 0, buffer.Length);
                buffer = buffer.Reverse().ToArray();
                Cgm_Elements.Last().colourSelectionMode = buffer[1] == 0 ? "indexed colour mode" : "direct colour mode";
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

        public void CGM_t_SVG(List<Cgm_Element> Cgm_Elements)
        {
            #region MyRegion
            bool pathNew = true;

            XmlDocument cgm_svg = new XmlDocument();

            XmlNode path = cgm_svg.CreateElement("path");

            Cgm_Element dd = Cgm_Elements.First(fd => fd.page_height > 0 && fd.page_width > 0);

            cgm_svg.LoadXml(String.Format("<svg  height=\"{0}mm\" width=\"{1}mm\" viewBox =\" {2} {2} {3} {4} \"/>",
                dd.page_height * dd.scaleFactor,
                dd.page_width * dd.scaleFactor,
                0,
                dd.page_width,
                dd.page_height
                ));
            Cgm_Element prevCgm = new Cgm_Element(); 
            #endregion
           
           if (dd.isbottomUp)
            {
                Cgm_Elements.ForEach(delegate(Cgm_Element cgm)
                {
                    #region MyRegion
                    cgm.page_height = dd.true_height;
                    if (cgm.elem_Name.EndsWith("TEXT"))
                    {
                        cgm.position.Y = dd.page_height - (cgm.position.Y + cgm.characterHeight);

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

                        cgm.points = cgm.points.Select((fd, idx) => idx <= 2 ? new PointF(fd.X, dd.page_height - fd.Y) : fd).ToList();
                        cgm.points[3] = p_start_alt;
                        cgm.points[4] = p_endin_alt;
                    }
                    else
                    {
                        cgm.points = cgm.points.Select(fd => new PointF(fd.X, dd.page_height - fd.Y)).ToList();
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

                       PointF center = PointF.Subtract(new PointF(dd.page_width,cgm.points[0].Y), new SizeF(cgm.points[0].X, 0));

                       p_start.X = dd.page_width - p_start.X;

                       p_endin.X = dd.page_width - p_endin.X;

                       PointF p_start_alt = PointF.Subtract(p_start, new SizeF(center.X, center.Y));
                       PointF p_endin_alt = PointF.Subtract(p_endin, new SizeF(center.X, center.Y));

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
            int cgm_idx = 0;
            int cgm_idx_abs = 0;
            cgm_svg.DocumentElement.SetAttribute("xmlns:xlink","http://www.w3.org/1999/xlink");
            foreach (Cgm_Element cgmElement in Cgm_Elements)
            {
                pathNew = true;
                bool close = cgmElement.isFig;
                bool isFigure = cgmElement.isFig;
                bool newRegion = false;
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
                            polyline.Append(String.Format("L {0} {1} ", pt.X, pt.Y));
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
                     
                    
                    path = cgm_svg.CreateElement("image");
                    cgm_svg.DocumentElement.AppendChild(path);
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("x")).Value = cgmElement.points[0].X.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("y")).Value = cgmElement.points[0].Y.ToString();

                    path.Attributes.Append(cgm_svg.CreateAttribute("width")).Value = cgmElement.rasterImage.Width.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("height")).Value = cgmElement.rasterImage.Height.ToString();
                    int scaleX = 1;
                    int scaleY = 1;
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
                    path.Attributes.Append(cgm_svg.CreateAttribute("transform")).Value = String.Format("scale({0},{1})", scaleX, scaleY);
                    

                    path.Attributes.Append(cgm_svg.CreateAttribute("xlink", "href", "http://www.w3.org/1999/xlink")).Value = string.Format("data:image/bmp;base64,{0}", cgmElement.raster2base64());

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
                    StringBuilder polyset = new StringBuilder();
                    foreach (PointF pt in cgmElement.points)
                    {
                        switch (cgmElement.polygonSetFlags[pt_idx])
                        {
                            case "invisible":

                                if (pt_idx == 0)
                                {
                                    polyset.Append(String.Format("M {0} {1} M", pt.X, pt.Y));
                                }
                                else
                                {
                                    polyset.Append(String.Format(" {0} {1} M", pt.X, pt.Y));
                                }

                                break;
                            case "visible":
                                if (pt_idx == 0)
                                {
                                    polyset.Append(String.Format("M {0} {1} L ", pt.X, pt.Y));
                                }
                                else
                                {
                                    polyset.Append(String.Format("{0} {1} ", pt.X, pt.Y));
                                }
                                break;
                            case "close,invisible":
                                if (pt_idx == 0)
                                {
                                    polyset.Append(String.Format("Z M {0} {1} M", pt.X, pt.Y));
                                }
                                else
                                {
                                    polyset.Append(String.Format("{0} {1} M{2} {3} M", pt.X, pt.Y, cgmElement.points.First().X, cgmElement.points.First().Y));
                                }

                                break;
                            case "close,visible":
                                if (pt_idx == 0)
                                {
                                    polyset.Append(String.Format("M {0} {1} Z M", pt.X, pt.Y));
                                }
                                else
                                {
                                    polyset.Append(String.Format("{0} {1} Z M", pt.X, pt.Y));
                                }
                                break;
                        }
                        pt_idx++;
                    }

                    path = cgm_svg.CreateElement("path");
                    path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                    path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                    path.Attributes["d"].Value = polyset.ToString();
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
                else if (cgmElement.elem_Name == ("ELLIPTICAL ARC"))
                {
                    #region MyRegion
                    if (!isFigure || path.Attributes["d"] == null)
                    {
                        path = cgm_svg.CreateElement("path");
                        cgm_svg.DocumentElement.AppendChild(path);
                        path.Attributes.Append(cgm_svg.CreateAttribute("elem_name")).Value = cgmElement.elem_Name;
                        path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();
                        path.Attributes.Append(cgm_svg.CreateAttribute("d"));
                    }
                    path.Attributes.Append(cgm_svg.CreateAttribute("idx")).Value = cgm_idx.ToString();

                    #region MyRegion
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

                    string cx = cgmElement.points[0].X.ToString();
                    string cy = cgmElement.points[0].Y.ToString();

                    path.Attributes["delta_start"].Value = cgmElement.points[3].ToString();
                    path.Attributes["delta_end"].Value = cgmElement.points[4].ToString();
                    path.Attributes["p1"].Value = cgmElement.points[1].ToString();
                    path.Attributes["p2"].Value = cgmElement.points[2].ToString();
                    path.Attributes["center"].Value = cgmElement.points[0].ToString();
                    #endregion

                    PointF p_start = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[3].X, cgmElement.points[3].Y));
                    PointF p_end = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[4].X, cgmElement.points[4].Y));

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
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke-width")).Value = "2";
                    //cgm_svg.DocumentElement.AppendChild(ee);
                    #endregion
                    ///////////////////////////////////////////////////////
                    float slope_p = 0;
                    float bint_p = 0;
                    double angle_P1 = 0;
                    double angle_P2 = 0;

                    distance_180(cgmElement.points[0], p_start, out angle_P1, out slope_p, out bint_p);

                    p_start = finfPontOnElispe((float)(angle), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_start.X, p_start.Y, float.IsInfinity(slope_p) ? p_start.Y : p_start.X);

                    distance_180(cgmElement.points[0], p_end, out angle_P2, out slope_p, out bint_p);
                    p_end = finfPontOnElispe((float)((angle)), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_end.X, p_end.Y, float.IsInfinity(slope_p) ? p_end.Y : p_end.X);

                    angle_LEN = angle_P2 - angle_P1;

                    getArcParams_ell((float)angle_dir, (float)angle_LEN, out path_len, out dir);

                    path.Attributes["angle_dir"].Value = Math.Round(angle_dir, 2).ToString();
                    path.Attributes["angle_ends"].Value = Math.Round(angle_3, 2).ToString();
                    path.Attributes["angle_len"].Value = Math.Round(angle_LEN, 2).ToString();

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


                    string arcTO = string.Format(" M {0} {1} A {2} {3} {6} {8} {7} {4} {5} ", p_start.X, p_start.Y, rx, ry, p_end.X, p_end.Y, angle_s, dir, path_len);
                    path.Attributes["d"].Value += arcTO;
                    #endregion
                }
                else if (cgmElement.elem_Name == ("ELLIPTICAL ARC CLOSE"))
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

                    string cx = cgmElement.points[0].X.ToString();
                    string cy = cgmElement.points[0].Y.ToString();




                    path.Attributes["delta_start"].Value = cgmElement.points[3].ToString();
                    path.Attributes["delta_end"].Value = cgmElement.points[4].ToString();
                    path.Attributes["p1"].Value = cgmElement.points[1].ToString();
                    path.Attributes["p2"].Value = cgmElement.points[2].ToString();
                    path.Attributes["center"].Value = cgmElement.points[0].ToString();


                    PointF p_start = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[3].X, cgmElement.points[3].Y));
                    PointF p_end = PointF.Add(cgmElement.points[0], new SizeF(cgmElement.points[4].X, cgmElement.points[4].Y));


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
                    ee.Attributes.Append(cgm_svg.CreateAttribute("stroke-width")).Value = "2";
                    //cgm_svg.DocumentElement.AppendChild(ee);
                    #endregion
                    ///////////////////////////////////////////////////////
                    float slope_p = 0;
                    float bint_p = 0;
                    double angle_P1 = 0;
                    double angle_P2 = 0;

                    distance_180(cgmElement.points[0], p_start, out angle_P1, out slope_p, out bint_p);

                    p_start = finfPontOnElispe((float)(angle), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_start.X, p_start.Y, float.IsInfinity(slope_p) ? p_start.Y : p_start.X);


                    distance_180(cgmElement.points[0], p_end, out angle_P2, out slope_p, out bint_p);
                    p_end = finfPontOnElispe((float)((angle)), (float)rx, (float)ry, cgmElement.points[0].X, cgmElement.points[0].Y, slope_p, p_end.X, p_end.Y, float.IsInfinity(slope_p) ? p_end.Y : p_end.X);

                    angle_LEN = angle_P2 - angle_P1;

                    getArcParams_cir((float)angle_LEN, out path_len, out dir);


                    path.Attributes["angle_dir"].Value = Math.Round(angle_dir, 2).ToString();
                    path.Attributes["angle_ends"].Value = Math.Round(angle_3, 2).ToString();
                    path.Attributes["angle_len"].Value = Math.Round(angle_LEN, 2).ToString();

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


                    string arcTO = string.Format(" M {0} {1} A {2} {3} {6} {8} {7} {4} {5} ", p_start.X, p_start.Y, rx, ry, p_end.X, p_end.Y, angle_s, dir, path_len);
                    path.Attributes["d"].Value += arcTO;

                    if (cgmElement.cir_arc_closure == "chord closure")
                    {
                        path.Attributes["d"].Value += string.Format("L {0} {1} Z ", p_start.X, p_start.Y);
                    }
                    else if (cgmElement.cir_arc_closure == "pieclosure")
                    {
                        path.Attributes["d"].Value += string.Format("L {0} {1} {2} {3} Z ", cgmElement.points[0].X, cgmElement.points[0].Y, p_start.X, p_start.Y);
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
                    path.Attributes["x"].Value = (cgmElement.position.X).ToString();
                    path.Attributes["y"].Value = (cgmElement.position.Y + (cgmElement.characterHeight)).ToString();
                    cgm_svg.DocumentElement.AppendChild(path);
                    if (cgmElement.characterOrientation != null)
                    {
                        if (cgmElement.characterOrientation[3] > 0)
                        {
                            path.Attributes["transform"].Value = String.Format("rotate(-90, {0}, {1})", cgmElement.position.X + cgmElement.delta_width / 10, cgmElement.position.Y + cgmElement.characterHeight);
                        }
                    }

                    #endregion
                }
                else if (cgmElement.elem_Name == "NEW REGION")
                {
                    #region MyRegion
                    pathNew = false;
                    newRegion = true;
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
                    newRegion = false;
                    prevCgm = cgmElement;
                    cgm_idx++;
                    if (path.Attributes["style"] == null)
                    {
                        path.Attributes.Append(cgm_svg.CreateAttribute("style"));
                    }
                    #region Apply Style
                    
                    if (cgmElement.isFilledArea())
                    {
                        path.Attributes["style"].Value += string.Format("stroke:#{0};", cgmElement.edgeColor.Name.Substring(2));
                        if (cgmElement.elem_Name == "RESTRICTED TEXT")
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
                            if (isFigure)
                            {
                                path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");                                
                            }
                            else
                            {
                                path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");
                            }
                            
                            path.Attributes["style"].Value += string.Format("fill:#{0};", cgmElement.fillColor.Name.Substring(2));
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility));
                        }
                        else if (cgmElement.fill_style == "hollow")
                        {
                            path.Attributes["style"].Value += string.Format("fill:none;");
                            path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth);
                        }
                        else
                        {
                            if (isFigure)
                            {
                                path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");
                            }
                            else
                            {
                                path.Attributes["style"].Value += string.Format("fill-rule:evenodd;");
                            }
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
                            path.Attributes["style"].Value += string.Format("font-size:{0};", cgmElement.delta_height);
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
                                path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.edgeWidth * Convert.ToInt32(cgmElement.edgeVisibility) );
                            }
                            else
                            {
                                path.Attributes["style"].Value += string.Format("stroke:#{0};", cgmElement.strokeColor.Name.Substring(2));
                                path.Attributes["style"].Value += string.Format("stroke-width:{0};", cgmElement.strokeWidth);
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
            cgm_svg.Save(@"C:\Users\795627\Desktop\cmg_svg.svg");
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
            angle = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;
            int sign = Math.Sign(angle);
            slope =   ((p2.Y - p1.Y) / (p2.X - p1.X));
            b_int = p1.X;
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

        public void getNextMetaLongLenth(BinaryReader br, byte[] buffer, out int paramLen)
        {
            buffer = new byte[2];
            br.Read(buffer, 0, buffer.Length);

            byte hbyte = buffer[0];
            byte lbyte = buffer[1];


            hbyte = buffer[0];
            lbyte = buffer[1];

           
            paramLen = BitConverter.ToUInt16(buffer.Reverse().ToArray(), 0);
            paramLen = paramLen & 0x7FFF;

          
        }

        public List<Cgm_Element> getNextMetaElement(ref BinaryReader br, ref byte[] buffer, out int bitsread, ref int paramLen, out byte elemclass, out byte elemId, out string elemName, List<Cgm_Element> Cgm_Elements)
        {


            if (paramLen % 2 > 0)
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
                strokeWidth = lastElem.strokeWidth,
                edgeVisibility = lastElem.edgeVisibility,
                characterHeight = lastElem.characterHeight,
                characterOrientation = lastElem.characterOrientation,
                characterColor = lastElem.characterColor,
                page_width = lastElem.page_width,
                page_height = lastElem.page_height,
                true_height = lastElem.true_height,
                true_width = lastElem.true_width,
                bgColor = lastElem.bgColor,
                mitreLimit = lastElem.mitreLimit,
                fill_style = lastElem.fill_style,
                vdcType = lastElem.vdcType,
                vdc_integer_precision = lastElem.vdc_integer_precision,
                integer_precision = lastElem.integer_precision,
                vdc_real_precision = lastElem.vdc_real_precision,
                real_precision = lastElem.real_precision,
                colour_precision = lastElem.colour_precision,
                colourSelectionMode = lastElem.colourSelectionMode,
                colorModel = lastElem.colorModel,
                colorTable = lastElem.colorTable,
                isFig = lastElem.isFig,
                scaleFactor = lastElem.scaleFactor,
                isleftRight = lastElem.isleftRight,
                realType = lastElem.realType,
                vdc_realType = lastElem.vdc_realType,
                isbottomUp = lastElem.isbottomUp
            });

            Cgm_Elements.Last().elemParams = new byte[paramLen];
            br.Read(Cgm_Elements.Last().elemParams, 0, paramLen);
            br.BaseStream.Seek(-paramLen, SeekOrigin.Current);

            return Cgm_Elements;

        }

        void getellipse(Cgm_Element cgmElement, out double rx, out double ry, out double angle, out double angle_2)
        {
            double angle_p1 = 0;
            double angle_p2 = 0;
            float slope_D1;
            float slope_D2;
            float b_1;
            float b_2;

            rx = distance_180(cgmElement.points[0], cgmElement.points[1], out angle_p1, out slope_D1, out b_1);
            ry = distance_180(cgmElement.points[0], cgmElement.points[2], out angle_p2, out slope_D2, out b_2);
            angle_2 = (angle_p2 - angle_p1);
            PointF rP_minor = cgmElement.points[2];
            PointF rP_major = cgmElement.points[1];
            
            if ( (int)(Math.Cos(Math.Abs(angle_2 * Math.PI /180)) * 100) != 0 )
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
                double dist_EC = distance_180(cgmElement.points[0] ,E, out angle, out slope_D2, out b_2);
                angle_2 = 90;
            }
            else
            {
                if (rx < ry)
                {
                    angle = rx;
                    rx = ry;
                    ry = angle;
                    angle = angle_p2;
                }
                else
                {
                    angle = angle_p1;
                }                
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
                    xr1 = h - 1;
                    xr0 = h - (d3 + 1);
                    xr0 = Math.Min(xMax, xr0) - 2;
                }
                else if (h < xMax)
                {
                    xr0 = h - 1;
                    xr1 = h + (d3 + 1);
                    xr1 = Math.Max(xMax, xr1);
                }
            }
            else if (float.IsInfinity(m))
            {
                if (float.IsNegativeInfinity(m))
                {
                    xr1 = k;
                    xr0 = k - d3;
                    
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
                    xr0 = h - (d3 + 1);
                    xr0 = Math.Min(xMax, xr0) - 2;
                }
                else if (h < xMax)
                {
                    xr0 = h;
                    xr1 = h + (d3 + 1);
                    xr1 = Math.Max(xMax, xr1);
                }
            }
            float upperLimit = (xr1 - xr0 + 1) * 1;
            range = Enumerable.Range((int)xr0-1, (int)(upperLimit)).Select(fd => (float)(fd + (1 / 1))).ToArray();
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
               // Console.WriteLine(res.ToString() + "\t" + x + "\t" + y);
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
                        path_len = angle_LEN > 180 ? '0' : '1';
                        dir = '0';
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
