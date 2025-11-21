public class PhysicsSystem
{
    private float terminalVelocity = 1000;
    public void Update()
    {
        float deltaTime = Raylib.GetFrameTime();

        foreach (var e in Game.GetEntities())
        {
            var physicsBody = e.GetComponentFast<PhysicsBody>();
            if (physicsBody == null || !physicsBody.useGravity) continue;

            var collider = e.GetComponentFast<Collider>();
            if (collider.colliderType == ColliderType.Static) continue;

            physicsBody.acceleration = physicsBody.gravity * deltaTime * physicsBody.weight;

            if (physicsBody.useGravity)
                physicsBody.velocity.Y += physicsBody.acceleration.Y * (int)physicsBody.physicsContext;

            //Clampa maxhastigheten 
            physicsBody.velocity.Y = Raymath.Clamp(physicsBody.velocity.Y, -terminalVelocity, terminalVelocity);

            //Uppdatera positionen
            // e.transform.position += physicsBody.velocity * Raylib.GetFrameTime() * 100;

        }
    }
}