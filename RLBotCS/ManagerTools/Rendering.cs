using rlbot.flat;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.TCP;

namespace RLBotCS.ManagerTools
{
    public class Rendering
    {
        private readonly RenderingSender _renderingSender;
        private Dictionary<int, Dictionary<int, List<ushort>>> _clientRenderTracker = [];

        public Rendering(TcpMessenger tcpMessenger)
        {
            _renderingSender = new RenderingSender(tcpMessenger);
        }

        private ushort? RenderItem(RenderTypeUnion renderItem)
        {
            return renderItem.Value switch
            {
                Line3DT { Start: var start, End: var end, Color: var color }
                    => _renderingSender.AddLine3D(
                        FlatToModel.ToVectorFromT(start),
                        FlatToModel.ToVectorFromT(end),
                        FlatToModel.ToColor(color)
                    ),
                PolyLine3DT { Points: var points, Color: var color }
                    => _renderingSender.AddLine3DSeries(
                        points.Select(FlatToModel.ToVectorFromT).ToList(),
                        FlatToModel.ToColor(color)
                    ),
                String2DT
                {
                    Text: var text,
                    X: var x,
                    Y: var y,
                    Foreground: var foreground,
                    Background: var background,
                    HAlign: var hAlign,
                    VAlign: var vAlign,
                    Scale: var scale
                }
                    => _renderingSender.AddText2D(
                        text,
                        x,
                        y,
                        FlatToModel.ToColor(foreground),
                        FlatToModel.ToColor(background),
                        (byte)hAlign,
                        (byte)vAlign,
                        scale
                    ),
                String3DT
                {
                    Text: var text,
                    Position: var position,
                    Foreground: var foreground,
                    Background: var background,
                    HAlign: var hAlign,
                    VAlign: var vAlign,
                    Scale: var scale
                }
                    => _renderingSender.AddText3D(
                        text,
                        FlatToModel.ToVectorFromT(position),
                        FlatToModel.ToColor(foreground),
                        FlatToModel.ToColor(background),
                        (byte)hAlign,
                        (byte)vAlign,
                        scale
                    ),
                _ => null
            };
        }

        public void AddRenderGroup(int clientId, int renderId, List<RenderMessageT> renderItems)
        {
            var clientRenders = _clientRenderTracker.GetValueOrDefault(
                clientId,
                new Dictionary<int, List<ushort>>()
            );

            // clear the previous render group
            if (clientRenders.TryGetValue(renderId, out List<ushort> previousRenderGroup))
            {
                foreach (ushort renderItem in previousRenderGroup)
                {
                    _renderingSender.RemoveRenderItem(renderItem);
                }
            }

            var renderGroup = new List<ushort>();

            foreach (RenderMessageT renderItem in renderItems)
            {
                if (RenderItem(renderItem.Variety) is ushort renderItemId)
                {
                    renderGroup.Add(renderItemId);
                }
            }
            _renderingSender.Send();

            // add to the tracker
            clientRenders[renderId] = renderGroup;
            _clientRenderTracker[clientId] = clientRenders;
        }

        public void RemoveRenderGroup(int clientId, int renderId)
        {
            if (_clientRenderTracker.TryGetValue(clientId, out Dictionary<int, List<ushort>> clientRenders))
            {
                if (clientRenders.TryGetValue(renderId, out List<ushort> renderItems))
                {
                    foreach (ushort renderItem in renderItems)
                    {
                        _renderingSender.RemoveRenderItem(renderItem);
                    }

                    // remove the renderId from the client
                    clientRenders.Remove(renderId);
                    _renderingSender.Send();
                }
            }
        }

        public void ClearClientRenders(int clientId)
        {
            if (_clientRenderTracker.TryGetValue(clientId, out Dictionary<int, List<ushort>> clientRenders))
            {
                // tell the game to remove all the renders
                foreach (int renderId in clientRenders.Keys)
                {
                    foreach (ushort renderItem in clientRenders[renderId])
                    {
                        _renderingSender.RemoveRenderItem(renderItem);
                    }
                }

                // remove the client from the tracker
                _clientRenderTracker.Remove(clientId);
                _renderingSender.Send();
                Console.WriteLine($"Client {clientId} renders cleared");
            }
        }

        public void ClearAllRenders()
        {
            // tell the game to remove all the renders
            // todo: we might not be able to remove all the renders in one tick
            foreach (Dictionary<int, List<ushort>> clientRenders in _clientRenderTracker.Values)
            {
                foreach (int renderId in clientRenders.Keys)
                {
                    foreach (ushort renderItem in clientRenders[renderId])
                    {
                        _renderingSender.RemoveRenderItem(renderItem);
                    }
                }
            }

            // clear the tracker
            _clientRenderTracker.Clear();
            _renderingSender.Send();

            Console.WriteLine("All renders cleared");
        }
    }
}
