using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;


public sealed class Tema : Scene
{
    private const string Restart = "repornește jocul";

   
    private const float ArenaHalfX = 6.0f;
    private const float StartZ = 8.0f;
    private const float GoalZ = -8.0f;

    // Jucătorul 
    private SceneObject _playerBody = null!;
    private PlayerController _player = null!;

    // Obstacolele mobile
    private readonly List<(SceneObject Obj, Tween Anim)> _obstacles = new();

    
    private int _score;
    private int _hits;

    public override string Name => "tema: evitare obstacole";

    public override string Status => $"Scor: {_score} | Lovituri: {_hits}";

    public override IEnumerable<string> ActionNames => new[]
    {
        Actions.Left, Actions.Right, Actions.Forward, Actions.Back, Actions.Jump, Restart
    };

    public override void Load(Input input)
    {
        
        CameraPosition = new Vector3(0.0f, 9.5f, 14.5f);
        CameraTarget = new Vector3(0.0f, 0.0f, -1.0f);

       
        input.Bind(Actions.Left, Keys.A, Keys.Left);
        input.Bind(Actions.Right, Keys.D, Keys.Right);
        input.Bind(Actions.Forward, Keys.W, Keys.Up);
        input.Bind(Actions.Back, Keys.S, Keys.Down);
        input.Bind(Actions.Jump, Keys.Space);
        input.Bind(Restart, Keys.R);

       
        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh disk = AddMesh(Shapes.ShadedCylinder(0.5f, 0.02f, 24));

        
        AddObject("podea", grid);

        SceneObject goalZone = AddObject("zona sosire", cube);
        goalZone.Transform.Position = new Vector3(0.0f, 0.01f, GoalZ);
        goalZone.Transform.Scale = new Vector3(ArenaHalfX * 2.0f, 0.02f, 1.8f);
        goalZone.Tint = new Color4(0.25f, 0.85f, 0.35f, 1.0f);

       
        SceneObject shadow = AddObject("umbra jucătorului", disk);
        shadow.Tint = new Color4(0.05f, 0.05f, 0.08f, 1.0f);

        _playerBody = AddObject("jucător", cube);
        _playerBody.Transform.Position = new Vector3(0.0f, 0.5f, StartZ);

        _player = new PlayerController(
            _playerBody,
            shadow,
            new Vector2(-ArenaHalfX + 0.5f, GoalZ),
            new Vector2(ArenaHalfX - 0.5f, StartZ)
        );

        
        AddObstacle(
            name: "obstacol 1",
            mesh: cube,
            from: new Vector3(-4.2f, 0.35f, 4.0f),
            to: new Vector3(4.2f, 0.35f, 4.0f),
            scale: new Vector3(3.2f, 0.7f, 0.8f),
            duration: 1.8f,
            ease: Easing.EaseInOut,
            color: new Color4(0.95f, 0.25f, 0.25f, 1.0f)
        );

       
        AddObstacle(
            name: "obstacol 2A",
            mesh: cube,
            from: new Vector3(4.5f, 0.6f, 0.5f),
            to: new Vector3(-1.0f, 0.6f, 0.5f),
            scale: new Vector3(2.4f, 1.2f, 0.9f),
            duration: 1.4f,
            ease: Easing.Linear,
            color: new Color4(0.95f, 0.55f, 0.15f, 1.0f)
        );

        AddObstacle(
            name: "obstacol 2B",
            mesh: cube,
            from: new Vector3(-4.5f, 0.6f, -1.5f),
            to: new Vector3(1.0f, 0.6f, -1.5f),
            scale: new Vector3(2.4f, 1.2f, 0.9f),
            duration: 1.4f,
            ease: Easing.Linear,
            color: new Color4(0.95f, 0.55f, 0.15f, 1.0f)
        );

     
        AddObstacle(
            name: "obstacol 3",
            mesh: cube,
            from: new Vector3(-4.5f, 0.5f, -4.5f),
            to: new Vector3(4.5f, 0.5f, -4.5f),
            scale: new Vector3(2.6f, 1.0f, 0.8f),
            duration: 1.15f,
            ease: Easing.EaseIn,
            color: new Color4(0.85f, 0.25f, 0.65f, 1.0f)
        );
    }

    private void AddObstacle(string name, Mesh mesh, Vector3 from, Vector3 to, Vector3 scale, float duration, EasingFunction ease, Color4 color)
    {
        SceneObject obs = AddObject(name, mesh);
        obs.Transform.Position = from;
        obs.Transform.Scale = scale;
        obs.Tint = color;

        var tween = new Tween(from, to, duration, ease, pingPong: true);
        _obstacles.Add((obs, tween));
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (input.WasPressed(Restart))
        {
            _score = 0;
            _hits = 0;
            _player.Reset();
            foreach (var (_, anim) in _obstacles) anim.Restart();
            ConsolePanel.PrintMessage("jocul a fost resetat");
        }

        foreach (var (obj, anim) in _obstacles)
        {
            anim.Update(dt);
            obj.Transform.Position = anim.Value;
        }

       
        _player.Update(dt, input);

      
        foreach (var (obs, _) in _obstacles)
        {
            if (CheckCollision(_playerBody.Transform, obs.Transform))
            {
                _hits++;
                _player.Reset();
                ConsolePanel.PrintMessage($"ai lovit {obs.Name}! Înapoi la start.");
                break;
            }
        }

        
        if (_player.Position.Z <= GoalZ + 0.6f)
        {
            _score++;
            _player.Reset();
            ConsolePanel.PrintMessage($"Bravo! Ai traversat arena! Scor: {_score}");
        }
    }

    private static bool CheckCollision(Transform3D a, Transform3D b)
    {
        Vector3 halfA = a.Scale * 0.45f; 
        Vector3 halfB = b.Scale * 0.48f;

        return MathF.Abs(a.Position.X - b.Position.X) < (halfA.X + halfB.X) &&
               MathF.Abs(a.Position.Y - b.Position.Y) < (halfA.Y + halfB.Y) &&
               MathF.Abs(a.Position.Z - b.Position.Z) < (halfA.Z + halfB.Z);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Scor / Lovituri: {_score} / {_hits}";
        yield return $"Jucător        : ({_player.Position.X:F2}, {_player.Position.Y:F2}, {_player.Position.Z:F2}) — {_player.State}";
    }
}