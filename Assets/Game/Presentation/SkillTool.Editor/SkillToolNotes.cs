using System.Collections.Generic;
using Evermeadow.Core.Skills;

namespace Evermeadow.Presentation.SkillTool.Editor
{
    /// <summary>
    /// Hover explanations (tooltips) of the Skill Tool, in Vietnamese, kept in one place so the wording can be edited without
    /// touching the node code. Looked up by field / output name; a "Kind.Name" entry overrides a shared name whose meaning
    /// differs per node (e.g. MoveType of MoveControl vs CameraAction).
    /// </summary>
    /// <example>
    /// <code>
    /// field.tooltip = SkillToolNotes.Field(SkillNodeKind.Hit, "SteadyDuration");
    /// edgeLabel.text = SkillToolNotes.Trigger(SkillTrigger.Hit);
    /// </code>
    /// </example>
    public static class SkillToolNotes
    {
        /// <summary>Owner field of the Start node.</summary>
        public const string Owner =
            "OWNER — kỹ năng dành cho ai.\nPlayer: người chơi dùng (gán id vào đòn / Skill Stone).\nCreature: quái / NPC dùng, do Enemy AI chọn.";

        /// <summary>Text shown on an edge for a trigger.</summary>
        public static string Trigger(SkillTrigger t)
        {
            switch (t)
            {
                case SkillTrigger.End: return "Khi node cha kết thúc";
                case SkillTrigger.Hit: return "Mỗi lần trúng một creature";
                case SkillTrigger.Found: return "Khi tìm thấy";
                case SkillTrigger.NotFound: return "Khi không tìm thấy";
                default: return "Khi node cha bắt đầu";
            }
        }

        /// <summary>Hover text of an edge label.</summary>
        public const string EdgeLabel =
            "Điều kiện để node con bắt đầu. Bấm để đổi.\nSau khi điều kiện xảy ra, node con còn chờ thêm ActionDelay của chính nó.";

        /// <summary>Hover text of a node kind (title bar and palette).</summary>
        public static string Node(SkillNodeKind kind)
        {
            switch (kind)
            {
                case SkillNodeKind.Start:
                    return "START — điểm bắt đầu kỹ năng (id 0, không xóa được).\nCung cấp các giá trị biết được lúc tung kỹ năng (id, cấp, chủ, mục tiêu " +
                           "ngoài…) để node khác nối dây vào dùng. Nối dây luồng từ Start sang các node chạy đầu tiên.";
                case SkillNodeKind.Animation:
                    return "ANIMATION — phát một animation của chủ kỹ năng.";
                case SkillNodeKind.Effect:
                    return "EFFECT — tạo hiệu ứng hình (prefab) tại một điểm, có thể bám theo, xoay, quay quanh mục tiêu.";
                case SkillNodeKind.Search:
                    return "SEARCH — tìm creature trong một vùng (cung tròn / hộp).\nDây ra có thể chọn \"Khi tìm thấy\" / \"Khi không tìm thấy\". " +
                           "Kết quả: Count, CreatureGIDs, CreaturePositions.";
                case SkillNodeKind.Hit:
                    return "HIT — vùng gây sát thương, đánh một lần hoặc kéo dài (Steady, lặp lại theo RepeatTime).\n" +
                           "Dây ra có thể chọn \"Mỗi lần trúng một creature\" để chạy node con cho từng lần trúng (vd Knockback, Buff).";
                case SkillNodeKind.Move:
                    return "MOVE — di chuyển chủ kỹ năng tới một điểm đích (lướt, nhảy).";
                case SkillNodeKind.MoveControl:
                    return "MOVE CONTROL — điều khiển việc tự di chuyển / xoay của chủ kỹ năng khi node chạy (khóa hướng, khóa di chuyển, đổi tốc).";
                case SkillNodeKind.LookAt:
                    return "LOOK AT — chủ kỹ năng quay nhìn về một creature hoặc một điểm.";
                case SkillNodeKind.CameraAction:
                    return "CAMERA ACTION — rung / zoom / di chuyển camera.";
                case SkillNodeKind.Knockback:
                    return "KNOCKBACK — đẩy hoặc kéo một creature (thường nối từ Hit với điều kiện \"Mỗi lần trúng\" và TargetCreatureGID = LastHitCreatureGID).";
                case SkillNodeKind.Buff:
                    return "BUFF — gắn một buff / debuff lên một creature.";
                case SkillNodeKind.End:
                    return "END — kết thúc kỹ năng khi tới (sau ActionDelay): animation của chủ kỹ năng về idle, nhân vật hết đòn " +
                           "(được điều khiển lại, bắt đầu tính cửa sổ nối đòn), node chưa bắt đầu bị bỏ; Hit / Effect… đang chạy vẫn chạy " +
                           "hết thời lượng. Không có node con. Nhiều End được (mỗi nhánh một End), End nào tới trước thắng. " +
                           "Kỹ năng không có End thì kết thúc khi mọi node xong.";
                default:
                    return kind.ToString();
            }
        }

        /// <summary>Hover text of a field or output.</summary>
        public static string Field(SkillNodeKind kind, string name)
        {
            if (Notes.TryGetValue(kind + "." + name, out string own)) return own;
            if (Notes.TryGetValue(name, out string shared)) return shared;
            if (name.StartsWith("Center") && Notes.TryGetValue(name.Substring(6), out string pivot)) return "(Tâm knockback) " + pivot;
            return name;
        }

        private static readonly Dictionary<string, string> Notes = new Dictionary<string, string>
        {
            // ---------------------------------------------------------------- common
            ["ActionDelay"] = "ACTION DELAY — số giây chờ sau khi điều kiện của dây vào xảy ra (node cha bắt đầu / kết thúc / mỗi lần trúng…) rồi node này mới chạy.",
            ["CanCancel"] = "CAN CANCEL — bật: node bị dừng khi kỹ năng bị hủy (bị ngắt, né…). Tắt: node chạy tiếp tới hết dù kỹ năng bị hủy.",
            ["UseAttackSpeed"] = "USE ATTACK SPEED — bật: ActionDelay và thời lượng của node co giãn theo tốc đánh của chủ kỹ năng.",
            ["NodeID"] = "NODE ID — số của node này (số trước dấu ':' trên tiêu đề).",

            // ---------------------------------------------------------------- pivot / direction / area
            ["PivotType"] = "PIVOT TYPE — điểm gốc gắn với: Creature (một creature, chọn bằng PivotCreatureGID) hoặc Position (một tọa độ, PivotPosition).",
            ["PivotCreatureGID"] = "PIVOT CREATURE GID — creature làm điểm gốc. -1 = chủ kỹ năng. Thường nối dây từ OwnerGID, ExternalTargetGID hay LastHitCreatureGID.",
            ["PivotPosition"] = "PIVOT POSITION — tọa độ làm điểm gốc khi PivotType = Position (x, y mặt chơi; z chiều sâu).",
            ["PivotOffsetType"] = "PIVOT OFFSET TYPE — hướng của độ lệch: None (theo trục thế giới), LookOfTarget (theo hướng nhìn của creature gốc), LookOfOwner (theo hướng nhìn của chủ kỹ năng).",
            ["PivotOffsetNormalized"] = "PIVOT OFFSET NORMALIZED — độ lệch theo tỉ lệ kích thước creature gốc (0,0,0 = không lệch).",
            ["PivotOffset"] = "PIVOT OFFSET — độ lệch thêm (ô tile) từ điểm gốc, theo hướng của PivotOffsetType. Ví dụ 0,0,4 = phía trước 4 ô.",
            ["SpaceType"] = "SPACE TYPE — Local: hướng tính theo hướng nhìn của điểm gốc. World: hướng tính theo trục thế giới.",
            ["SpaceFrontDirection"] = "SPACE FRONT DIRECTION — hướng \"phía trước\" của vùng / hiệu ứng trong không gian đã chọn. 0,0,1 = thẳng phía trước.",
            ["AreaType"] = "AREA TYPE — hình vùng: Arc (cung tròn: SubValue = góc°, bán kính) hoặc Box (hộp: SubValue = rộng, dài, cao).",
            ["SubValue"] = "SUB VALUE — kích thước vùng. Arc: x = góc (360 = tròn), y = bán kính (ô). Box: x = rộng, y = dài, z = cao. Xem hình xem trước bên dưới.",
            ["AreaScale"] = "AREA SCALE — hệ số phóng to / thu nhỏ cả vùng.",

            // ---------------------------------------------------------------- Start outputs
            ["SkillID"] = "SKILL ID — id của kỹ năng (tên file).",
            ["SkillLevel"] = "SKILL LEVEL — cấp kỹ năng lúc tung.",
            ["CastRatio"] = "CAST RATIO — tỉ lệ tụ / niệm lúc tung (‰ hoặc theo quy ước của hệ gọi).",
            ["SkillInputType"] = "SKILL INPUT TYPE — cách kỹ năng được kích hoạt: Press, Hold, Release, Channel.",
            ["IsChanneling"] = "IS CHANNELING — kỹ năng đang được duy trì (giữ nút).",
            ["OwnerGID"] = "OWNER GID — id thực thể của chủ kỹ năng lúc chạy (Long).",
            ["OwnerTID"] = "OWNER TID — mã loại của chủ kỹ năng (Creature: 100–999).",
            ["OwnerPosition"] = "OWNER POSITION — vị trí chủ kỹ năng lúc tung.",
            ["OwnerDirection"] = "OWNER DIRECTION — hướng nhìn của chủ kỹ năng lúc tung.",
            ["OwnerLevel"] = "OWNER LEVEL — cấp của chủ kỹ năng.",
            ["ExternalTargetGID"] = "EXTERNAL TARGET GID — mục tiêu do hệ gọi kỹ năng truyền vào (quái: người chơi; người chơi: mục tiêu đang khóa). -1 nếu không có.",
            ["ExternalPosition"] = "EXTERNAL POSITION — điểm do hệ gọi truyền vào (vd điểm nhắm).",

            // ---------------------------------------------------------------- Animation
            ["AnimationType"] = "ANIMATION — slot animation cần phát (attack1, skill2, idle2…), chọn từ danh sách slot chung cho mọi rig " +
                                "(player, quái): kỹ năng gán cho ai cũng chạy được. Rig phát clip cùng tên slot (theo look vũ khí nếu có); " +
                                "rig thiếu clip thì bỏ qua và báo ở Console.",
            ["IsLoop"] = "IS LOOP — phát lặp. Node kết thúc khi bị dừng (hủy kỹ năng) thay vì khi clip hết.",
            ["TimeScale"] = "TIME SCALE — tốc độ phát (1 = bình thường).",

            // ---------------------------------------------------------------- Effect
            ["PrefabName"] = "PREFAB NAME — key của prefab hiệu ứng (tên file, vd vfx_cmn_smear_spear) trong Assets/Game/Art/VFX/<Loại>/. Bấm để mở danh sách effect + màn hình preview.",
            ["Effect.Duration"] = "DURATION — hiệu ứng tồn tại bao lâu (giây). Node kết thúc khi hết thời gian này.",
            ["Scale"] = "SCALE — kích thước hiệu ứng.",
            ["RotateMotion"] = "ROTATE MOTION — kiểu xoay: None, Spin (tự xoay quanh mình).",
            ["Effect.RotateDegreePerSec"] = "ROTATE DEGREE PER SEC — tốc độ xoay (độ / giây).",
            ["IsReverse"] = "IS REVERSE — xoay ngược chiều.",
            ["IsRotateSteady"] = "IS ROTATE STEADY — giữ hướng cố định, không xoay theo điểm gốc.",
            ["BindType"] = "BIND TYPE — cách hiệu ứng bám: None (đứng yên tại chỗ tạo), AttachToTarget (dính vào điểm gốc), Chase (đuổi theo), Orbit (quay quanh).",
            ["ChaseSpeed"] = "CHASE SPEED — tốc độ đuổi theo (BindType = Chase).",
            ["OrbitRadius"] = "ORBIT RADIUS — bán kính quay quanh (BindType = Orbit).",
            ["OrbitDegreePerSec"] = "ORBIT DEGREE PER SEC — tốc độ quay quanh (độ / giây).",
            ["OrbitStartAngle"] = "ORBIT START ANGLE — góc bắt đầu khi quay quanh.",
            ["Effect.IsReleaseWhenOwnerDie"] = "IS RELEASE WHEN OWNER DIE — xóa hiệu ứng ngay khi chủ kỹ năng chết.",
            ["LastEffectPosition"] = "LAST EFFECT POSITION — vị trí cuối cùng của hiệu ứng.",

            // ---------------------------------------------------------------- Search
            ["SearchObjectFlag"] = "SEARCH OBJECT FLAG — đối tượng được xét: Enemies, Allies, Owner, Neutral (chọn nhiều).",
            ["TargetTID"] = "TARGET TID — chỉ tìm creature có mã loại này (100–999). 0 = mọi loại. Nhiều con cùng loại → kết quả là danh sách.",
            ["IsPrioritizeOwnerLook"] = "IS PRIORITIZE OWNER LOOK — ưu tiên creature ở phía chủ kỹ năng đang nhìn.",
            ["MaxCount"] = "MAX COUNT — số creature tối đa lấy về (gần nhất trước).",
            ["Count"] = "COUNT — số creature tìm được.",
            ["CreatureGIDs"] = "CREATURE GIDS — danh sách id các creature tìm được (gần nhất trước).",
            ["CreaturePositions"] = "CREATURE POSITIONS — vị trí các creature tìm được.",

            // ---------------------------------------------------------------- Hit
            ["Hit.SearchObjectFlag"] = "SEARCH OBJECT FLAG — đối tượng có thể bị trúng: Enemies, Allies, Owner, Neutral.",
            ["IsSteady"] = "IS STEADY — bật: vùng chạm tồn tại trong SteadyDuration và đánh lặp mỗi RepeatTime. Tắt: đánh một lần.",
            ["IsFollowPivot"] = "IS FOLLOW PIVOT — vùng chạm kéo dài đi theo điểm gốc.",
            ["SteadyDuration"] = "STEADY DURATION — vùng chạm kéo dài bao lâu (giây).",
            ["RepeatTime"] = "REPEAT TIME — khoảng cách giữa các lần đánh khi kéo dài (giây).",
            ["Hit.IsReleaseWhenOwnerDie"] = "IS RELEASE WHEN OWNER DIE — xóa vùng chạm khi chủ kỹ năng chết.",
            ["IsRotate"] = "IS ROTATE — vùng chạm kéo dài xoay quanh điểm gốc.",
            ["Radius"] = "RADIUS — bán kính quỹ đạo xoay (IsRotate).",
            ["Hit.RotateDegreePerSec"] = "ROTATE DEGREE PER SEC — tốc độ xoay của vùng (độ / giây).",
            ["StartAngle"] = "START ANGLE — góc bắt đầu khi xoay.",
            ["FactorType"] = "FACTOR TYPE — công thức sát thương (Combat định nghĩa).",
            ["FactorRatio"] = "FACTOR RATIO — hệ số sát thương, 10000 = 100%.",
            ["SkillMaterialType"] = "SKILL MATERIAL TYPE — chất liệu của đòn, quyết định âm thanh / tia lửa khi trúng.",
            ["HitEffectAddressable"] = "HIT EFFECT ADDRESSABLE — key prefab hiệu ứng hiện tại chỗ trúng (thường thuộc loại Hit). Bấm để chọn từ danh sách effect có preview.",
            ["HitEffectDuration"] = "HIT EFFECT DURATION — hiệu ứng trúng tồn tại bao lâu (giây).",
            ["BlockerTIDs"] = "BLOCKER TIDS — mã loại các vật cản chặn đòn, cách nhau dấu phẩy.",
            ["InstantKill"] = "INSTANT KILL — bật: có thể giết ngay.",
            ["InstantKillHpThreshold"] = "INSTANT KILL HP THRESHOLD — ngưỡng máu (‰) để được giết ngay.",
            ["InstantKillRatio"] = "INSTANT KILL RATIO — tỉ lệ giết ngay (‰).",
            ["LastHitCreatureGID"] = "LAST HIT CREATURE GID — creature vừa bị trúng (dùng với dây \"Mỗi lần trúng\").",
            ["LastHitCreaturePosition"] = "LAST HIT CREATURE POSITION — vị trí creature vừa bị trúng.",
            ["DistinctHitCreatureGIDs"] = "DISTINCT HIT CREATURE GIDS — các creature đã trúng (mỗi con một lần).",
            ["DistinctHitCreaturePositions"] = "DISTINCT HIT CREATURE POSITIONS — vị trí các creature đã trúng.",
            ["IndistinctHitCount"] = "INDISTINCT HIT COUNT — tổng số lần trúng (tính cả trúng lặp).",
            ["LastInstantKillPosition"] = "LAST INSTANT KILL POSITION — vị trí lần giết ngay gần nhất.",

            // ---------------------------------------------------------------- Move / MoveControl / LookAt
            ["MovingType"] = "MOVING TYPE — đường đi: Straight (thẳng) hoặc Jump (nhảy vòng cung).",
            ["MoveStandard"] = "MOVE STANDARD — Value tính theo: Time (giây để tới đích) hoặc Speed (ô / giây).",
            ["Value"] = "VALUE — giây (Time) hoặc tốc độ (Speed), theo MoveStandard.",
            ["StartPosition"] = "START POSITION — vị trí lúc bắt đầu di chuyển.",
            ["EndPosition"] = "END POSITION — vị trí đích.",
            ["MoveControl.MoveType"] = "MOVE TYPE — FixDirection: khóa hướng quay; LockMove: khóa tự di chuyển; SpeedRatio: đổi tốc di chuyển theo Ratio.",
            ["Ratio"] = "RATIO — hệ số tốc độ khi node chạy (1 = bình thường).",
            ["MoveControl.Duration"] = "DURATION — thời gian điều khiển (giây). 0 = tới khi node bị dừng.",
            ["Direction"] = "DIRECTION — hướng nhìn sau khi quay.",

            // ---------------------------------------------------------------- CameraAction
            ["ActionType"] = "ACTION TYPE — Shake (rung), Zoom, Move (dịch camera).",
            ["CameraAction.Duration"] = "DURATION — tác động kéo dài bao lâu (giây).",
            ["FollowType"] = "FOLLOW TYPE — camera bám theo: Player, Owner (chủ kỹ năng) hoặc None.",
            ["CameraAction.MoveType"] = "MOVE TYPE — hướng của MoveOffset: PlayerLookVector (theo hướng nhìn người chơi) hoặc Fixed.",
            ["MoveOffset"] = "MOVE OFFSET — độ dịch camera.",
            ["DampingX"] = "DAMPING X — độ trễ bám theo trục X.",
            ["DampingY"] = "DAMPING Y — độ trễ bám theo trục Y.",
            ["DampingZ"] = "DAMPING Z — độ trễ bám theo trục Z.",
            ["IsShakeBroadcast"] = "IS SHAKE BROADCAST — rung cho cả người chơi khác thấy (không chỉ chủ kỹ năng).",
            ["ShakeAmplitude"] = "SHAKE AMPLITUDE — biên độ rung.",
            ["ShakeFrequency"] = "SHAKE FREQUENCY — tần số rung.",
            ["ZoomSize"] = "ZOOM SIZE — mức zoom (Zoom).",
            ["KnockbackOffset"] = "KNOCKBACK OFFSET — độ giật camera.",

            // ---------------------------------------------------------------- Knockback / Buff
            ["TargetCreatureGID"] = "TARGET CREATURE GID — creature bị tác động. -1 = chủ kỹ năng. Thường nối dây từ LastHitCreatureGID của Hit.",
            ["Distance"] = "DISTANCE — quãng bị đẩy / kéo (ô).",
            ["Knockback.Duration"] = "DURATION — thời gian bị đẩy (giây).",
            ["KnockbackType"] = "KNOCKBACK TYPE — PushInDirection (đẩy theo SpaceFrontDirection), PushFromCenter (đẩy ra xa tâm), PullToCenter (kéo về tâm).",
            ["StackCount"] = "STACK COUNT — số tầng buff thêm vào.",
            ["BuffType"] = "BUFF TYPE — id của buff (vd Buff 00288).",
            ["Buff.Duration"] = "DURATION — buff kéo dài bao lâu (giây).",
            ["Parameter0"] = "PARAMETER 0–5 — tham số riêng của buff (ý nghĩa do bảng buff định nghĩa).",
            ["Parameter1"] = "PARAMETER 0–5 — tham số riêng của buff.",
            ["Parameter2"] = "PARAMETER 0–5 — tham số riêng của buff.",
            ["Parameter3"] = "PARAMETER 0–5 — tham số riêng của buff.",
            ["Parameter4"] = "PARAMETER 0–5 — tham số riêng của buff.",
            ["Parameter5"] = "PARAMETER 0–5 — tham số riêng của buff."
        };
    }
}
