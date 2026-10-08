using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace Tycoon
{
    public sealed partial class GameHud : MonoBehaviour
    {
        void OpenCrewMenu()
        {
            ShowScreen(ScreenMode.Crew);
            RefreshCrewMenu();
            Select(crewContent.GetComponentsInChildren<Button>().FirstOrDefault(x => x.interactable) ?? crewBack);
        }
        void BackToPauseMenu()
        {
            ShowScreen(ScreenMode.Pause);
            Select(continueButton);
        }
        void RefreshCrewMenu()
        {
            foreach (Transform child in crewContent)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            crewStatuses.Clear();
            var crews = game.CrewStates;
            if (crews.Count == 0)
            {
                SetText(crewSummary, "Trạm cấp 3 và 30 công việc tự làm sẽ mở điểm thuê đội.");
                ((RectTransform)crewContent).sizeDelta = new(0, 140);
                var empty = Text("NoCrew", crewContent, "Bạn có thể tự sản xuất, vận chuyển và phục vụ.", 24,
                    TextAnchor.MiddleCenter, Muted);
                Rect(empty.rectTransform, new(0, 1), new(1, 1), new(20, -130), new(-20, -20));
                return;
            }
            SetText(crewSummary, "Nâng cấp áp dụng đúng nghề và khu • tiền được trừ ngay khi chọn.");
            int index = 0;
            foreach (var crew in crews)
            {
                var row = Panel("CrewRow_" + crew.id, crewContent, Card);
                Rect((RectTransform)row.transform, new(0, 1), new(1, 1), new(0, -index * 232 - 220), new(0,
                    -index * 232));
                string title = RoleLabel(crew.role) + " • " + AreaLabel(crew.area) + " • " + crew.count + " người";
                var name = Text("CrewName", row.transform, title, 25, TextAnchor.MiddleLeft, Ink);
                Rect(name.rectTransform, new(0, 1), new(1, 1), new(18, -53), new(-18, -8));
                var status = Text("CrewStatus", row.transform, "", 20, TextAnchor.MiddleLeft, Muted);
                Rect(status.rectTransform, new(0, 1), new(1, 1), new(18, -110), new(-18, -57));
                crewStatuses[crew.id] = status;
                CrewButton(row.transform, 0, UpgradeLabel(crew, "speed"),
                    crew.speedLevel < 3 ? () => UpgradeCrew(crew.id, "speed") : null);
                bool hasCarry = crew.role is not "Cashier" and not "Driver" and not "Repairer";
                CrewButton(row.transform, 1, hasCarry ? UpgradeLabel(crew, "carry") : "Không dùng sức mang",
                hasCarry && crew.carryLevel < 3 ? () => UpgradeCrew(crew.id, "carry") : null);
                CrewButton(row.transform, 2, crew.role == "Driver" ? "Một tài xế / xe" : UpgradeLabel(crew, "count"),
                crew.count < WorkforceRules.Limit(crew.id) ? () => UpgradeCrew(crew.id, "count") : null);
                index++;
            }
            ((RectTransform)crewContent).sizeDelta = new(0, index * 232);
            crewContent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            RefreshStatus();
        }
        string UpgradeLabel(CrewState crew, string type)
        {
            int level = type == "speed" ? crew.speedLevel : type == "carry" ? crew.carryLevel : crew.count;
            string label = type == "speed" ? "Tốc độ" : type == "carry" ? "Sức mang" : "Số người";
            int maximum = type == "count" ? WorkforceRules.Limit(crew.id) : 3;
            if (level >= maximum) return label + " • TỐI ĐA";
            return (type == "count" ? "Thêm người • " + (level + 1) : label + " cấp " + (level + 1)) +
            "\n" + game.CrewUpgradeCost(crew.id, type).ToString("N0") + " xu";
        }
        void CrewButton(Transform parent, int column, string label, UnityEngine.Events.UnityAction click)
        {
            var button = Button(parent, "CrewUpgrade", click);
            button.GetComponentInChildren<Text>().text = label;
            button.GetComponentInChildren<Text>().fontSize = 20;
            button.interactable = click != null;
            Rect((RectTransform)button.transform, new(column / 3f, 0), new((column + 1) / 3f, 0), new(12, 14),
                new(-12, 98));
        }
        void UpgradeCrew(string id, string type)
        {
            bool changed = game.UpgradeCrew(id, type);
            game.Say(changed ? "Đã nâng đội " + (Definitions.Upgrade(id)?.label ?? id)
                : "Không đủ xu hoặc nâng cấp đã tối đa.");
            RefreshCrewMenu();
            Select(crewBack);
        }
        public static string AreaLabel(string area) => area switch
        {
            "farm" => "Nông trại", "farm_shop" => "Cửa hàng nông sản", "processing" => "Chế biến",
            "supermarket" or "market" => "Siêu thị", "bakery" => "Tiệm bánh", "restaurant" => "Nhà hàng", _ => area
        };
        static string RoleLabel(string role) => role switch
        {
            "Farmer" => "Nông dân", "AnimalWorker" => "Chăm vật nuôi", "Restocker" => "Xếp hàng",
                "Cashier" => "Bán hàng",
            "Processor" => "Chế biến", "Cook" => "Đầu bếp / thợ bánh", "Waiter" => "Phục vụ",
                "Transporter" => "Vận chuyển",
            "Repairer" => "Kỹ thuật viên sửa chữa", "Loader" => "Bốc hàng", "Driver" => "Tài xế", _ => role
        };
    }
}
