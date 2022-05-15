/**
 * Base Class of all Ground Floors
 */

using UnityEngine;

namespace Game.Logic
{
  public class GroundFloor : Entity
  {
    #region PUBLIC METHODS

    #region PUBLIC OVERRIDDENS
    /**
     * Collsion Handler for any Entity
     * - Normal Entity(Except Weapon) will call weapon's onCollision() during its handling
     */
    public override void onCollision(Entity a_pTrigger,                   // who has triggered this Hit
                                     CollisionHandler a_pCollidedPart     // collided part
                                    )
    {
      switch(a_pTrigger.EntityType)
      {
      // when a weapon hit this ground
      case EntityTypes.ENTITY_TYPES_WEAPON:
        {
          if(WeaponTypes.WEAPON_TYPES_SHOOTWEAPON != ((Weapon)a_pTrigger).WeaponType   // NOT shooting weapon
             &&                                                                        // and
             CharacterTypes.CHARACTER_TYPES_MONSTER != ((Weapon)a_pTrigger).OwnerType  // NOT monster's weapon
             &&                                                                        // and
             CharacterTypes.CHARACTER_TYPES_BOSS != ((Weapon)a_pTrigger).OwnerType     // NOT boss' weapon
             &&                                                                        // and
             true == ((Weapon)a_pTrigger).isInUse                                      // In Any Attacking state
            )
          {
            // let the weapon handle this hit
            ((Weapon)a_pTrigger).onCollision(this, a_pCollidedPart);
          }

          break;
        }

      default:
        {
          break;
        }
      }
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS

    #region PROTECTED METHODS

    #region PROTECTED OVERRIDDENS
    /**
     * doResetInEditor() is called when the user hits the Reset button in the Inspector's context menu
     * or when adding the component the first time 
     * This function is only called in editor mode 
     * doResetInEditor() is most commonly used to give good default values in the inspector
     */
    protected override void doResetInEditor()
    {
      // set the correct entity type
      EntityType = EntityTypes.ENTITY_TYPES_GROUND;

      // remove all useless animations
      SpawnAnimations = null;
      HitAnimations = null;
      StruckAnimations = null;
      DeadAnimations = null;
    }

    // simulate the spawn animation
    protected override void doSpawnAnimation()
    {
      // no spawn animation simulation
    }

    // determine if spawn process will need to wait for completion of spawn animation: keyframed or simulated
    protected override bool waitForSpawnAnimation()
    {
      return false;
    }

    #endregion // PROTECTED OVERRIDDENS 

    #endregion // PROTECTED METHODS
  }

} // namespace Game.Logic
