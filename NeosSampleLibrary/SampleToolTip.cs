using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseX;
using FrooxEngine;

namespace NeosSampleLibrary
{
    // An example of a tooltip component, which plugs into the common tool system
    // The common tool provides a set of common behaviors like grabbing, touching, tooltip selecting, menus and so on
    // while the tooltips can provide specific behaviors on top of that, using either primary or secondary action

    public class SampleToolTip : ToolTip
    {
        // Property that holds the radius in which the cubes are spawned around the tip
        public readonly Sync<float> SpawnRadius;

        // Position of the tip of the tool in local coordinate system of the object
        public override float3 LocalTip => float3.Forward * 0.075f;

        // This is called right after an instance of the component is constructed in the system and can be used to configure the default state
        // This is useful for setting deterministic default values.
        protected override void OnAwake()
        {
            base.OnAwake();

            // default radius
            SpawnRadius.Value = 0.05f;
        }

        // This is called when the component is attached to a slot
        // This can be used to construct the tool visual programmatically
        protected override void OnAttach()
        {
            // The ToolTip base class has behaviors too, so make sure they're executed
            base.OnAttach();

            // Add a child slot which will hold the visuals
            var visual = Slot.AddSlot("Visual");

            // Let's attach a collider, which will ensure it can be grabbed and equipped
            var coneCollider = visual.AttachComponent<ConeCollider>();
            coneCollider.Radius.Value = 0.015f;
            coneCollider.Height.Value = 0.05f;

            // Rotate the visual (cones are pointing upwards)
            visual.LocalRotation = floatQ.Euler(90, 0, 0);
            // Offset it a little forwards
            visual.LocalPosition += float3.Forward * 0.05f;

            // Let's create a new material for the tooltip model
            var material = visual.AttachComponent<PBS_Metallic>();
            material.AlbedoColor.Value = new color(1f, 0.75f, 0f); // make the material golden to distinguish the tooltip a bit

            // Attach a cone mesh. The AttachMesh<MeshType> is a shorthand for attaching MeshRenderer
            // a mesh provider and setting up the references appropriatelly
            var cone = visual.AttachMesh<ConeMesh>(material);

            // Set the mesh parameters
            cone.RadiusBase.Value = 0.015f;
            cone.Height.Value = 0.05f;
        }

        // This is called every update when the tool is equipped for the user that has the tooltip equipped
        public override void Update(float primaryStrength, float2 secondaryAxis, Digital primary, Digital secondary)
        {
            // Pressing the primary button (usually trigger) will increase probability of spawning a cube each frame, up to 10 % when fully pressed
            if (RandomX.Chance(primaryStrength * 0.5f))  // Alternate way of writing: if(RandomX.Value < primaryStrength * 0.1f)
                SpawnCube();
        }

        void SpawnCube()
        {
            // Let's spawn a cube!
            var slot = World.AddSlot("Cube"); // let's add a new empty slot

            // let's attach mesh renderer which will render the cube
            var meshRenderer = slot.AttachComponent<MeshRenderer>();
            // also add a box collider, which will allow interactions with the cube
            var boxCollider = slot.AttachComponent<BoxCollider>();
            // grabbable component will ensure that the cube can be grabbed and moved around by tools that implement the grabbable system
            var grabbable = slot.AttachComponent<Grabbable>();
            grabbable.Scalable = true; // allow scaling

            // let's set the box collider size. It's the same as default value, so this is redundant, but shows how it works
            boxCollider.Size.Value = float3.One;

            // Now we need a mesh and a material to assign to the mesh renderer. Since they'll all be cubes and will use the same
            // material, we only want to add one instance per world to save resources. Luckily Neos has a nice engine for that
            var boxMesh = World.GetSharedComponentOrCreate<BoxMesh>("SampleToolTip_Mesh", mesh =>
            {
                // This will get called when the component is created, so we can initialize it however we want
                mesh.Size.Value = float3.One; // also same as default value, but good for demonstration
            });

            var material = World.GetSharedComponentOrCreate<PBS_Metallic>("SampleToolTip_Material", mat =>
            {
                mat.AlbedoColor.Value = new color(1f, 0.7f, 0f); // nice golden color
                mat.Smoothness.Value = 0.9f; // Very shiny!
                mat.Metallic.Value = 0f;
            });

            // Let's assign the references to the mesh renderer, so it knows which mesh to render with which material
            meshRenderer.Mesh.Target = boxMesh;
            meshRenderer.Material.Target = material;

            // Lastly let's position the new object at random point around the tip and scale it as well
            slot.GlobalPosition = Tip + RandomX.InsideUnitSphere * SpawnRadius * (float)MathX.Sin(Time.WorldTime * MathX.PI * 0.5f);
            slot.LocalScale = float3.One * RandomX.Range(0.005f, 0.02f);
        }

        // The tooltip can also override methods to react to specific press
        public override void OnSecondaryPress()
        {
            // Spawn a bunch at once!
            var number = RandomX.Range(20, 40);
            for (int i = 0; i < number; i++)
                SpawnCube();
        }
    }
}
