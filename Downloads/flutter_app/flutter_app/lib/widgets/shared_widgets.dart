import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

// ─── Custom App Bar ───────────────────────────────────────────────────────────

class FinanceAppBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;
  final VoidCallback? onBack;
  final Widget? action;
  final Widget? leading;

  const FinanceAppBar({super.key, required this.title, this.onBack, this.action, this.leading});

  @override
  Size get preferredSize => const Size.fromHeight(56);

  @override
  Widget build(BuildContext context) {
    return AppBar(
      backgroundColor: AppColors.cardBg,
      elevation: 0,
      leading: leading ??
          GestureDetector(
            onTap: onBack ?? () => Navigator.pop(context),
            child: Container(
              margin: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppColors.inputBg,
                borderRadius: BorderRadius.circular(50),
              ),
              child: const Icon(Icons.arrow_back_ios_new, size: 16, color: AppColors.textDark),
            ),
          ),
      title: Text(title,
          style: const TextStyle(
              fontSize: 16, fontWeight: FontWeight.w700, color: AppColors.textDark)),
      actions: [
        action ??
            Container(
              margin: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppColors.inputBg,
                borderRadius: BorderRadius.circular(50),
              ),
              child: const Icon(Icons.notifications_outlined,
                  size: 18, color: AppColors.textDark),
            ),
      ],
    );
  }
}

// ─── Bottom Nav ───────────────────────────────────────────────────────────────

class FinanceBottomNav extends StatelessWidget {
  const FinanceBottomNav({super.key});

  @override
  Widget build(BuildContext context) {
    final icons = [Icons.home_outlined, Icons.location_on_outlined,
        Icons.sync_outlined, Icons.bar_chart_outlined, Icons.more_horiz];
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.cardBg,
        border: Border(top: BorderSide(color: AppColors.border)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 10),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: icons.asMap().entries.map((e) => Icon(
            e.value,
            color: e.key == 0 ? AppColors.primary : AppColors.textMuted,
            size: 22,
          )).toList(),
        ),
      ),
    );
  }
}

// ─── Primary Button ───────────────────────────────────────────────────────────

class PrimaryButton extends StatelessWidget {
  final String text;
  final VoidCallback onPressed;
  final Color? color;

  const PrimaryButton({
    super.key,
    required this.text,
    required this.onPressed,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: color ?? AppColors.primary,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          padding: const EdgeInsets.symmetric(vertical: 15),
          elevation: 0,
        ),
        child: Text(text,
            style: const TextStyle(
                fontSize: 15, fontWeight: FontWeight.w700, color: Colors.white)),
      ),
    );
  }
}

// ─── Input Field ──────────────────────────────────────────────────────────────

class FinanceInput extends StatelessWidget {
  final String hint;
  final bool obscure;
  final Widget? suffix;
  final TextEditingController? controller;

  const FinanceInput({
    super.key,
    required this.hint,
    this.obscure = false,
    this.suffix,
    this.controller,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: controller,
        obscureText: obscure,
        style: const TextStyle(fontSize: 14, color: AppColors.textDark),
        decoration: InputDecoration(
          hintText: hint,
          hintStyle: const TextStyle(color: AppColors.textMuted, fontSize: 14),
          suffixIcon: suffix,
          filled: true,
          fillColor: AppColors.inputBg,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: AppColors.border),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: AppColors.border),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
          ),
          contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        ),
      ),
    );
  }
}

// ─── Avatar Circle ────────────────────────────────────────────────────────────

class AvatarCircle extends StatelessWidget {
  final String initials;
  final double size;
  final Color color;

  const AvatarCircle({
    super.key,
    required this.initials,
    this.size = 40,
    this.color = AppColors.primary,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(color: color, shape: BoxShape.circle),
      alignment: Alignment.center,
      child: Text(initials,
          style: TextStyle(
              color: Colors.white,
              fontSize: size * 0.4,
              fontWeight: FontWeight.w700)),
    );
  }
}

// ─── Status Badge ─────────────────────────────────────────────────────────────

enum TransactionStatus { pending, sent, unpaid, paid }

class StatusBadge extends StatelessWidget {
  final TransactionStatus status;

  const StatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) {
    final data = {
      TransactionStatus.pending: (const Color(0xFFFEF3C7), const Color(0xFFD97706), 'Transaction Status: Pending'),
      TransactionStatus.sent:    (const Color(0xFFD1FAE5), const Color(0xFF059669), 'Transaction Status: Sent'),
      TransactionStatus.unpaid:  (const Color(0xFFFEE2E2), const Color(0xFFDC2626), 'Transaction Status: Unpaid'),
      TransactionStatus.paid:    (const Color(0xFFD1FAE5), const Color(0xFF059669), 'Transaction Status: Paid'),
    }[status]!;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
          color: data.$1, borderRadius: BorderRadius.circular(8)),
      child: Text(data.$3,
          style: TextStyle(
              color: data.$2, fontSize: 11, fontWeight: FontWeight.w600)),
    );
  }
}

// ─── Info Row ─────────────────────────────────────────────────────────────────

class InfoRow extends StatelessWidget {
  final String label;
  final String value;

  const InfoRow({super.key, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(label,
                  style: const TextStyle(
                      color: AppColors.textMuted, fontSize: 13)),
              Text(value,
                  style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textDark)),
            ],
          ),
          const SizedBox(height: 8),
          const Divider(height: 1, color: AppColors.border),
        ],
      ),
    );
  }
}

// ─── Coin Success Animation ───────────────────────────────────────────────────

class CoinAnimation extends StatefulWidget {
  const CoinAnimation({super.key});

  @override
  State<CoinAnimation> createState() => _CoinAnimationState();
}

class _CoinAnimationState extends State<CoinAnimation>
    with TickerProviderStateMixin {
  late AnimationController _c1, _c2;
  late Animation<double> _a1, _a2;

  @override
  void initState() {
    super.initState();
    _c1 = AnimationController(vsync: this, duration: const Duration(milliseconds: 800))
      ..repeat(reverse: true);
    _c2 = AnimationController(vsync: this, duration: const Duration(milliseconds: 800))
      ..repeat(reverse: true);
    _a1 = Tween<double>(begin: 0, end: -10).animate(
        CurvedAnimation(parent: _c1, curve: Curves.easeInOut));
    _a2 = Tween<double>(begin: -10, end: 0).animate(
        CurvedAnimation(parent: _c2, curve: Curves.easeInOut));
    Future.delayed(const Duration(milliseconds: 200), () {
      if (mounted) _c2.forward();
    });
  }

  @override
  void dispose() {
    _c1.dispose();
    _c2.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        AnimatedBuilder(
          animation: _a1,
          builder: (_, child) => Transform.translate(
            offset: Offset(0, _a1.value),
            child: const Text('🪙', style: TextStyle(fontSize: 44)),
          ),
        ),
        const SizedBox(width: 8),
        AnimatedBuilder(
          animation: _a2,
          builder: (_, child) => Transform.translate(
            offset: Offset(0, _a2.value),
            child: const Text('🪙', style: TextStyle(fontSize: 44)),
          ),
        ),
      ],
    );
  }
}
