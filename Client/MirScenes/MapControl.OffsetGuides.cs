using Client.MirGraphics;
using Client.MirObjects;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private void DrawOffsetGuides()
    {
        if (GraphicsTuning.OffsetGuides.Current == 0 || User == null) return;
        var screen = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);
        int drawn = 0;
        foreach (MapObject actor in Objects.Values)
        {
            if (!screen.Contains(actor.DrawLocation)) continue;
            Point offset = GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Pixels;
            Point raw = new(actor.DrawLocation.X - offset.X, actor.DrawLocation.Y - offset.Y);
            if (actor == User || offset != Point.Empty)
            {
                Guide(raw, actor.DrawLocation, Color.Cyan);
                if (actor is PlayerObject or MonsterObject or NPCObject && GraphicsTuning.ProjectedOffset.Pixels != Point.Empty)
                    Guide(actor.DrawLocation, GraphicsTuning.ProjectedOffset.Apply(actor.DrawLocation,
                        GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Linked), Color.LightSlateGray);
                if (++drawn >= 32) break;
            }
            if (actor.Light > 0)
            {
                Point origin = actor is SpellObject spell ? spell.LightPosition : ActorLightPosition(actor, ActorLightSource(actor));
                if (actor == User || origin != actor.DrawLocation)
                {
                    Guide(actor.DrawLocation, origin, Color.Gold);
                    if (++drawn >= 32) break;
                }
            }
        }

        Point cell = ToMouseLocation(User.CurrentLocation);
        cell.Offset(CellWidth, CellHeight);
        foreach (GraphicsLayer layer in GraphicsTuning.Layers)
        {
            Point offset = GraphicsTuning.Offset(layer).Pixels;
            if (offset == Point.Empty) continue;
            if (layer is GraphicsLayer.Ground or GraphicsLayer.Middle or GraphicsLayer.Front or GraphicsLayer.MapAnimation or GraphicsLayer.Weather)
                Guide(new Point(cell.X - OffSetX, cell.Y), new Point(cell.X - OffSetX + offset.X, cell.Y + offset.Y), Color.Violet);
            if (layer == GraphicsLayer.Background) Guide(Point.Empty, offset, Color.Violet);
        }
    }

    private static void Guide(Point raw, Point actual, Color colour)
    {
        var ui = DXManager.Device.Ui;
        ui.Fill(new Rectangle(raw.X - 2, raw.Y - 2, 5, 5), Color.FromArgb(225, Color.Black));
        DXManager.DrawLine(raw, actual, Color.FromArgb(210, colour));
        ui.Fill(new Rectangle(actual.X - 5, actual.Y, 11, 1), colour);
        ui.Fill(new Rectangle(actual.X, actual.Y - 5, 1, 11), colour);
    }
}
