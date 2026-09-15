using System; using System.IO; using System.Drawing; using System.Drawing.Drawing2D; using System.Drawing.Imaging; using System.Collections.Generic;
class MakeIcon {
 static void Main(string[] args) {
 int[] sizes={16,24,32,48,64,128,256}; var images=new List<byte[]>();
 foreach(int size in sizes) using(var b=new Bitmap(size,size)) {
 using(var g=Graphics.FromImage(b)) {
 g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/256f,size/256f);
 using(var path=new GraphicsPath()) { path.AddArc(8,8,56,56,180,90);path.AddArc(192,8,56,56,270,90);path.AddArc(192,192,56,56,0,90);path.AddArc(8,192,56,56,90,90);path.CloseFigure(); using(var brush=new SolidBrush(Color.FromArgb(29,49,76)))g.FillPath(brush,path); }
 using(var pen=new Pen(Color.FromArgb(116,148,172),10)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(48,62),new PointF(48,204),new PointF(206,204)});}
 using(var pen=new Pen(Color.White,16)){pen.LineJoin=LineJoin.Round;pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(65,171),new PointF(91,171),new PointF(119,92),new PointF(149,155),new PointF(198,66)});}
 using(var brush=new SolidBrush(Color.FromArgb(33,207,188)))g.FillEllipse(brush,182,50,32,32);
 }
 if(size==256)b.Save(args[1],ImageFormat.Png);
 using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);images.Add(ms.ToArray());}
 }
 using(var w=new BinaryWriter(File.Create(args[0]))) { w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;
 for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(images[i].Length);w.Write(offset);offset+=images[i].Length;}
 foreach(var bytes in images)w.Write(bytes);
 }
 }
}
