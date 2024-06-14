using rlbot.flat;
using RLBotSecret.Controller;
using RLBotSecret.Conversion;
using RLBotSecret.TCP;

namespace RLBotCS.MatchManagement
{
    public class Rendering
    {
        private readonly RenderingSender _renderingSender;
        private Dictionary<int, Dictionary<int, List<ushort>>> _clientRenderTracker =
            new Dictionary<int, Dictionary<int, List<ushort>>>();

        public Rendering(TcpMessenger tcpMessenger)
        {
            _renderingSender = new RenderingSender(tcpMessenger);
        }

        private ushort? RenderItem(RenderTypeUnion renderItem)
        {
            switch (renderItem.Type)
            {
                case RenderType.Line3D:
                    var line3D = renderItem.AsLine3D();

                    return _renderingSender.AddLine3D(
                        FlatToModel.ToVectorFromT(line3D.Start),
                        FlatToModel.ToVectorFromT(line3D.End),
                        FlatToModel.ToColor(line3D.Color)
                    );
                case RenderType.PolyLine3D:
                    var polyLine3D = renderItem.AsPolyLine3D();

                    return _renderingSender.AddLine3DSeries(
                        polyLine3D.Points.Select(FlatToModel.ToVectorFromT).ToList(),
                        FlatToModel.ToColor(polyLine3D.Color)
                    );
                case RenderType.String2D:
                    var string2D = renderItem.AsString2D();

                    return _renderingSender.AddText2D(
                        string2D.Text,
                        string2D.X,
                        string2D.Y,
                        FlatToModel.ToColor(string2D.Foreground),
                        FlatToModel.ToColor(string2D.Background),
                        (byte)string2D.HAlign,
                        (byte)string2D.VAlign,
                        string2D.Scale
                    );
                case RenderType.String3D:
                    var string3D = renderItem.AsString3D();

                    return _renderingSender.AddText3D(
                        string3D.Text,
                        FlatToModel.ToVectorFromT(string3D.Position),
                        FlatToModel.ToColor(string3D.Foreground),
                        FlatToModel.ToColor(string3D.Background),
                        (byte)string3D.HAlign,
                        (byte)string3D.VAlign,
                        string3D.Scale
                    );
                default:
                    return null;
            }
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
