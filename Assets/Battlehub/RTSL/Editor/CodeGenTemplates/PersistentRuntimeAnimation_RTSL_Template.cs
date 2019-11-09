﻿//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using ProtoBuf;
using Battlehub.RTEditor;
using System.Collections.Generic;
using System.Linq;
//<TEMPLATE_USINGS_END>
#else
using UnityEngine;
#endif

namespace Battlehub.RTSL.Internal
{
    [PersistentTemplate("Battlehub.RTEditor.RuntimeAnimation", 
        new[] { "Clips", "ClipIndex" }, new[] { "Battlehub.RTEditor.RuntimeAnimationClip" } )]
    public class PersistentRuntimeAnimation_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>

        [ProtoMember(1)]
        public long[] Clips;

        [ProtoMember(2)]
        public int ClipIndex;

        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);

            RuntimeAnimation animation = (RuntimeAnimation)obj;
            if (animation == null)
            {
                return;
            }

            IList<RuntimeAnimationClip> clips = animation.Clips;
            if(clips != null)
            {
                Clips = ToID(clips.ToArray());
            }

            ClipIndex = animation.ClipIndex;
        }

        public override object WriteTo(object obj)
        {
            obj = base.WriteTo(obj);
            RuntimeAnimation animation = (RuntimeAnimation)obj;
            if (animation == null)
            {
                return null;
            }


                        IList<RuntimeAnimationClip> clips = FromID<RuntimeAnimationClip>(Clips, animation.Clips.ToArray());
            animation.SetClips(clips, ClipIndex);
            return animation;
        }

        public override void GetDeps(GetDepsContext context)
        {
            base.GetDeps(context);
            AddDep(Clips, context);
        }

        public override void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            base.GetDepsFrom(obj, context);
            RuntimeAnimation o = (RuntimeAnimation)obj;
            if (obj == null)
            {
                return;
            }

            AddDep(o.Clips, context);
        }

        //<TEMPLATE_BODY_END>
#endif
    }
}


