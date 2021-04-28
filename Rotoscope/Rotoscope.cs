using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Rotoscope
{
    class Rotoscope
    {
        private List<LinkedList<Point>> draw = new List<LinkedList<Point>>();
        public LinkedList<Point> GetFromDrawList(int frame)
        {
            if (frame < 0 || draw.Count == 0 || draw.Count < frame)
                return null;

            //copy the list to ensure it is not overwritten
            return draw[frame];
        }

        public void AddToDrawList(int frame, Point p)
        {
            //if the frame doesn't exists yet, add it
            while (draw.Count < frame + 1)
                draw.Add(new LinkedList<Point>());

            // Add the mouse point to the list for the frame
            draw[frame].AddLast(p);
        }

        public void OnSaveRotoscope(XmlDocument doc, XmlNode node)
        {
            for (int frame = 0; frame < draw.Count; frame++)
            {
                // Create an XML node for the frame
                XmlElement element = doc.CreateElement("frame");
                element.SetAttribute("num", frame.ToString());

                node.AppendChild(element);

                //
                // Now save the point data for the frame
                //

                foreach (Point p in draw[frame])
                {
                    // Create an XML node for the point
                    XmlElement pElement = doc.CreateElement("point");

                    // Add attributes for the point
                    pElement.SetAttribute("x", p.X.ToString());
                    pElement.SetAttribute("y", p.Y.ToString());

                    // Append the node to the node we are nested inside.
                    element.AppendChild(pElement);
                }
            }
        }

        public void OnOpenRotoscope(XmlNode node)
        {

            //
            // Traverse the frame node 
            //
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "frame")
                {
                    LoadFrame(child);
                }
            }

        }

        private void LoadFrame(XmlNode node)
        {
            int frame = 0;
            // Get a list of all attribute nodes and the
            // length of that list
            foreach (XmlAttribute attr in node.Attributes)
            {
                if (attr.Name == "num")
                {
                    frame = Convert.ToInt32(attr.Value);
                }
            }

            //
            // Traverse the frame node 
            //
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "point")
                {
                    LoadPoint(frame, child);
                }
            }
        }

        public void ClearFrame(int frame)
        {
            if (frame >= 0 && draw.Count > frame)
                draw[frame].Clear();
        }


        private void LoadPoint(int frame, XmlNode node)
        {
            int x = 0;
            int y = 0;

            foreach (XmlAttribute attr in node.Attributes)
            {
                if (attr.Name == "x")
                {
                    x = Convert.ToInt32(attr.Value);
                }
                if (attr.Name == "y")
                {
                    y = Convert.ToInt32(attr.Value);
                }
            }

            AddToDrawList(frame, new Point(x, y));

        }
    }
}
