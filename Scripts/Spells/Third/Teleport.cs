using System;
using Server.Targeting;
using Server.Network;
using Server.Regions;
using Server.Items;

namespace Server.Spells.Third
{
	public class TeleportSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Teleport", "Rel Por",
				215,
				9031,
				Reagent.Bloodmoss,
				Reagent.MandrakeRoot
			);

		public override SpellCircle Circle { get { return SpellCircle.Third; } }

        public override void SelectTarget()
        {
            Caster.Target = new InternalSphereTarget(this);
        }

        public override void OnSphereCast()
        {
            if (SpellTarget != null)
            {
                if (SpellTarget is IPoint3D)
                {
                    Target((IPoint3D)SpellTarget);
                }
                else
                {
                    Caster.SendAsciiMessage("Invalid target");
                }
            }
            FinishSequence();
        }

	    public TeleportSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( Factions.Sigil.ExistsOn( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
				return false;
			}
			else if ( Server.Misc.WeightOverloading.IsOverloaded( Caster ) )
			{
				Caster.SendLocalizedMessage( 502359, "", 0x22 ); // Thou art too encumbered to move.
				return false;
			}

			return SpellHelper.CheckTravel( Caster, TravelCheckType.TeleportFrom );
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			IPoint3D orig = p;
			Map map = Caster.Map;

			SpellHelper.GetSurfaceTop( ref p );

			if ( Factions.Sigil.ExistsOn( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
			}
			else if ( Server.Misc.WeightOverloading.IsOverloaded( Caster ) )
			{
				Caster.SendLocalizedMessage( 502359, "", 0x22 ); // Thou art too encumbered to move.
			}
			else if ( !SpellHelper.CheckTravel( Caster, TravelCheckType.TeleportFrom ) )
			{
			}
			else if ( !SpellHelper.CheckTravel( Caster, map, new Point3D( p ), TravelCheckType.TeleportTo ) )
			{
			}
			else if ( map == null || !map.CanSpawnMobile( p.X, p.Y, p.Z ) )
			{
				Caster.SendLocalizedMessage( 501942 ); // That location is blocked.
			}
			else if ( SpellHelper.CheckMulti( new Point3D( p ), map ) )
			{
				Caster.SendLocalizedMessage( 501942 ); // That location is blocked.
			}
            else if (!CheckLineOfSight(p))
            {
                this.DoFizzle();
                Caster.SendAsciiMessage("Target is not in line of sight");
            }
			else if ( CheckSequence() )
			{
				SpellHelper.Turn( Caster, orig );

				Mobile m = Caster;

				Point3D from = m.Location;
				Point3D to = new Point3D( p );

				m.Location = to;
				m.ProcessDelta();

				if ( m.Player )
				{
					Effects.SendLocationParticles( EffectItem.Create( from, m.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023 );
					Effects.SendLocationParticles( EffectItem.Create(   to, m.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 5023 );
				}
				else
				{
					m.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
				}

				m.PlaySound( 0x1FE );

				IPooledEnumerable eable = m.GetItemsInRange( 0 );

				foreach ( Item item in eable )
				{
					if ( item is Server.Spells.Sixth.ParalyzeFieldSpell.InternalItem || item is Server.Spells.Fifth.PoisonFieldSpell.InternalItem || item is Server.Spells.Fourth.FireFieldSpell.FireFieldItem )
						item.OnMoveOver( m );
				}

				eable.Free();
			}

			FinishSequence();
		}

        private class InternalSphereTarget : Target
        {
            private TeleportSpell m_Owner;

            public InternalSphereTarget(TeleportSpell owner)
                : base(Core.ML ? 10 : 12, true, TargetFlags.None)
            {
                m_Owner = owner;
                m_Owner.Caster.SendAsciiMessage("Select target...");
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is IPoint3D)
                {
                    m_Owner.SpellTarget = o;
                    m_Owner.CastSpell();
                }
                else
                {
                    m_Owner.Caster.SendAsciiMessage("Invalid target");
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_Owner.SpellTarget == null)
                {
                    m_Owner.Caster.SendAsciiMessage("Targeting cancelled.");
                }
            }
        }

		public class InternalTarget : Target
		{
			private TeleportSpell m_Owner;

			public InternalTarget( TeleportSpell owner ) : base( Core.ML ? 11 : 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				IPoint3D p = o as IPoint3D;

				if ( p != null )
					m_Owner.Target( p );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}