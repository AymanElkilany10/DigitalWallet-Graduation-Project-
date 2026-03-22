import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';

// ─── Screen 1: Bill Selection ─────────────────────────────────────────────────

class PB1BillsScreen extends StatefulWidget {
  const PB1BillsScreen({super.key});

  @override
  State<PB1BillsScreen> createState() => _PB1BillsScreenState();
}

class _PB1BillsScreenState extends State<PB1BillsScreen> {
  int _selectedBill = 1; // Electricity

  final _bills = [
    {'icon': Icons.wifi, 'name': 'Internet Bill', 'color': AppColors.primary},
    {
      'icon': Icons.electric_bolt,
      'name': 'Electricity Bill',
      'color': const Color(0xFFF59E0B)
    },
    {
      'icon': Icons.water_drop,
      'name': 'Water Bill',
      'color': const Color(0xFF3B82F6)
    },
    {'icon': Icons.more_horiz, 'name': 'Other', 'color': AppColors.textMuted},
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: const FinanceAppBar(title: 'Pay Bill'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Your Bills',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 10),
            ..._bills.asMap().entries.map((e) {
              final i = e.key;
              final b = e.value;
              final selected = i == _selectedBill;
              return GestureDetector(
                onTap: () => setState(() => _selectedBill = i),
                child: Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
                  decoration: BoxDecoration(
                    color:
                        selected ? const Color(0xFFEEF2FF) : AppColors.inputBg,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                        color: selected ? AppColors.primary : AppColors.border),
                  ),
                  child: Row(
                    children: [
                      Icon(b['icon'] as IconData,
                          color: b['color'] as Color, size: 22),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(b['name'] as String,
                            style: const TextStyle(
                                fontWeight: FontWeight.w600, fontSize: 14)),
                      ),
                      Container(
                        width: 18,
                        height: 18,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(
                              color: selected
                                  ? AppColors.primary
                                  : AppColors.textMuted,
                              width: 2),
                          color:
                              selected ? AppColors.primary : Colors.transparent,
                        ),
                        child: selected
                            ? const Icon(Icons.check,
                                size: 10, color: Colors.white)
                            : null,
                      ),
                    ],
                  ),
                ),
              );
            }),
            const SizedBox(height: 16),
            const Text('Fill Details',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 10),
            const FinanceInput(hint: 'Company Name'),
            const FinanceInput(hint: 'Reference Number'),
            const FinanceInput(
              hint: 'Password',
              obscure: true,
              suffix: Icon(Icons.remove_red_eye_outlined,
                  color: AppColors.textMuted, size: 18),
            ),
            const SizedBox(height: 6),
            PrimaryButton(
              text: 'Next',
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const PB2ConfirmScreen()),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}

// ─── Screen 2: Confirm ────────────────────────────────────────────────────────

class PB2ConfirmScreen extends StatelessWidget {
  const PB2ConfirmScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: FinanceAppBar(
        title: 'Confirmation',
        leading: Container(
          margin: const EdgeInsets.all(8),
          decoration: const BoxDecoration(
            color: AppColors.primary,
            shape: BoxShape.circle,
          ),
          alignment: Alignment.center,
          child: const Text('W',
              style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                  fontSize: 16)),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            const SizedBox(height: 16),
            Container(
              width: 60,
              height: 60,
              decoration: const BoxDecoration(
                  color: AppColors.darkCard, shape: BoxShape.circle),
              child: const Icon(Icons.electric_bolt,
                  color: Colors.white, size: 30),
            ),
            const SizedBox(height: 10),
            const Text('Electricity Bill',
                style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 4),
            const Text('Daniel John',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
            const SizedBox(height: 10),
            const StatusBadge(status: TransactionStatus.unpaid),
            const SizedBox(height: 14),
            RichText(
              text: const TextSpan(
                children: [
                  TextSpan(
                    text: '\$350.00',
                    style: TextStyle(
                        fontSize: 30,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textDark),
                  ),
                  TextSpan(
                    text: ' USD',
                    style: TextStyle(fontSize: 14, color: AppColors.textMuted),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            const Text('Are you sure?',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary)),
            const SizedBox(height: 8),
            const Text(
              'Please make sure that you want to pay electricity bill',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 13, color: AppColors.textMuted),
            ),
            const SizedBox(height: 16),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              decoration: BoxDecoration(
                  color: AppColors.inputBg,
                  borderRadius: BorderRadius.circular(12)),
              child: const Column(
                children: [
                  InfoRow(label: 'Bill Number', value: '12569874554'),
                  InfoRow(label: 'Due Date', value: 'March 23, 2021'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              text: 'Pay Now',
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const PB3CongratsScreen()),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}

// ─── Screen 3: Congratulations ────────────────────────────────────────────────

class PB3CongratsScreen extends StatelessWidget {
  const PB3CongratsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: FinanceAppBar(
        title: 'Confirmation',
        leading: Container(
          margin: const EdgeInsets.all(8),
          decoration: const BoxDecoration(
            color: AppColors.primary,
            shape: BoxShape.circle,
          ),
          alignment: Alignment.center,
          child: const Text('W',
              style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                  fontSize: 16)),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const CoinAnimation(),
            const SizedBox(height: 16),
            const Text('Congratulations!',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary)),
            const SizedBox(height: 8),
            const Text(
              'Your electricity bill payment has been paid successfully.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 13, color: AppColors.textMuted),
            ),
            const SizedBox(height: 30),
            PrimaryButton(
              text: 'View Receipt',
              onPressed: () => showModalBottomSheet(
                context: context,
                isScrollControlled: true,
                backgroundColor: Colors.transparent,
                builder: (_) => Container(
                  decoration: const BoxDecoration(
                    color: AppColors.cardBg,
                    borderRadius:
                        BorderRadius.vertical(top: Radius.circular(24)),
                  ),
                  padding: const EdgeInsets.fromLTRB(18, 12, 18, 24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 40,
                        height: 4,
                        decoration: BoxDecoration(
                          color: AppColors.border,
                          borderRadius: BorderRadius.circular(2),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: AppColors.inputBg,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.center,
                          children: [
                            Container(
                              width: 50,
                              height: 50,
                              decoration: const BoxDecoration(
                                  color: AppColors.darkCard,
                                  shape: BoxShape.circle),
                              child: const Icon(Icons.electric_bolt,
                                  color: Colors.white, size: 24),
                            ),
                            const SizedBox(height: 8),
                            const Text('Electricity Bill',
                                style: TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 15)),
                            const SizedBox(height: 10),
                            const StatusBadge(status: TransactionStatus.paid),
                            const SizedBox(height: 10),
                            RichText(
                              textAlign: TextAlign.center,
                              text: const TextSpan(
                                children: [
                                  TextSpan(
                                    text: '\$350.00',
                                    style: TextStyle(
                                        fontSize: 28,
                                        fontWeight: FontWeight.w800,
                                        color: AppColors.textDark),
                                  ),
                                  TextSpan(
                                    text: ' USD',
                                    style: TextStyle(
                                        fontSize: 13,
                                        color: AppColors.textMuted),
                                  ),
                                ],
                              ),
                            ),
                            const SizedBox(height: 12),
                            const Divider(),
                            const InfoRow(
                                label: 'Bill Number', value: '12569874554'),
                            const InfoRow(
                                label: 'Date', value: 'March 23, 2021'),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      PrimaryButton(
                        text: 'Back to Home',
                        onPressed: () =>
                            Navigator.popUntil(context, (r) => r.isFirst),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}

// ─── Screen 4: Receipt ────────────────────────────────────────────────────────

class PB4ReceiptScreen extends StatelessWidget {
  const PB4ReceiptScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: const FinanceAppBar(title: 'Confirmation'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            const SizedBox(height: 12),
            const CoinAnimation(),
            const SizedBox(height: 8),
            const Text('Congratulations!',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary)),
            const SizedBox(height: 4),
            const Text(
              'Your electricity bill payment has been paid successfully.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
            const SizedBox(height: 16),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                  color: AppColors.inputBg,
                  borderRadius: BorderRadius.circular(16)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        width: 44,
                        height: 44,
                        decoration: const BoxDecoration(
                            color: AppColors.darkCard, shape: BoxShape.circle),
                        child: const Icon(Icons.electric_bolt,
                            color: Colors.white, size: 22),
                      ),
                      const SizedBox(width: 10),
                      const Text('Electricity Bill',
                          style: TextStyle(
                              fontWeight: FontWeight.w700, fontSize: 15)),
                    ],
                  ),
                  const SizedBox(height: 12),
                  const StatusBadge(status: TransactionStatus.paid),
                  const SizedBox(height: 10),
                  RichText(
                    text: const TextSpan(
                      children: [
                        TextSpan(
                          text: '\$350.00',
                          style: TextStyle(
                              fontSize: 28,
                              fontWeight: FontWeight.w800,
                              color: AppColors.textDark),
                        ),
                        TextSpan(
                          text: ' USD',
                          style: TextStyle(
                              fontSize: 13, color: AppColors.textMuted),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  const InfoRow(label: 'Bill Number', value: '12569874554'),
                  const InfoRow(label: 'Date', value: 'March 23, 2021'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            TextButton(
              onPressed: () => Navigator.popUntil(context, (r) => r.isFirst),
              child: const Text('Back to Home',
                  style: TextStyle(
                      color: AppColors.primary, fontWeight: FontWeight.w700)),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
