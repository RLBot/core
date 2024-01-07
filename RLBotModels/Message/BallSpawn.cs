namespace RLBotModels.Message
{
    public class BallSpawn : IMessage
    {
        public ushort actorId;
        public ushort commandId;
        public CollisionShapeUnion collisionShape;
    }
    public class CollisionShapeUnion
    {
        public CollisionShape Type;
        public object Value;

        public T As<T>() where T : class { return this.Value as T; }
        public BoxShape AsBoxShape() { return this.As<BoxShape>(); }
        public static CollisionShapeUnion FromBoxShape(BoxShape _boxshape) { return new CollisionShapeUnion { Type = CollisionShape.BoxShape, Value = _boxshape }; }
        public SphereShape AsSphereShape() { return this.As<SphereShape>(); }
        public static CollisionShapeUnion FromSphereShape(SphereShape _sphereshape) { return new CollisionShapeUnion { Type = CollisionShape.SphereShape, Value = _sphereshape }; }
        public CylinderShape AsCylinderShape() { return this.As<CylinderShape>(); }
        public static CollisionShapeUnion FromCylinderShape(CylinderShape _cylindershape) { return new CollisionShapeUnion { Type = CollisionShape.CylinderShape, Value = _cylindershape }; }
    }
    public enum CollisionShape : byte
    {
        BoxShape = 0,
        SphereShape = 1,
        CylinderShape = 2,
    };

    public class BoxShape
    {
        public float Length;
        public float Width;
        public float Height;

        public BoxShape()
        {
            this.Length = 0.0f;
            this.Width = 0.0f;
            this.Height = 0.0f;
        }
    }

    public class SphereShape
    {
        public float Diameter;

        public SphereShape()
        {
            this.Diameter = 0.0f;
        }
    }

    public class CylinderShape
    {
        public float Diameter;
        public float Height;

        public CylinderShape()
        {
            this.Diameter = 0.0f;
            this.Height = 0.0f;
        }
    }
    
}