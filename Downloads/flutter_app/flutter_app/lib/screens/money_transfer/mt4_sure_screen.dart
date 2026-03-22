import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';
import 'mt5_success_screen.dart';

class MT4SureScreen extends StatelessWidget {
  const MT4SureScreen({super.key});

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
            const AvatarCircle(initials: 'A', size: 64),
            const SizedBox(height: 10),
            const Text('Ahmed',
                style: TextStyle(
                    fontSize: 16, fontWeight: FontWeight.w700, color: AppColors.textDark)),
            const SizedBox(height: 4),
            const Text('p••••••p333',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
            const SizedBox(height: 10),
            const StatusBadge(status: TransactionStatus.pending),
            const SizedBox(height: 14),
            RichText(
              text: const TextSpan(
                children: [
                  TextSpan(
                    text: '\$250.00',
                    style: TextStyle(
                        fontSize: 32,
                        fontWeight: FontWeight.w800,
                        color: AppColors.primary),
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
              'We care about your privacy, please make sure that you want to transfer money.',
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
                  InfoRow(label: 'Card Type', value: 'Debit Card'),
                  InfoRow(label: 'Transfer Fee', value: '\$0.00 USD'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              text: 'Send Money',
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const MT5SuccessScreen()),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
