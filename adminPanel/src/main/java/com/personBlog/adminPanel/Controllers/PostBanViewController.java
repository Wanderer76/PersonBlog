package com.personBlog.adminPanel.Controllers;

import com.personBlog.adminPanel.Dto.PostBanRequestsViewModel;
import com.personBlog.adminPanel.Services.PostBanService;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
@RequestMapping("/admin/posts")
public class PostBanViewController {

    private final PostBanService postBanService;

    public PostBanViewController(PostBanService postBanService) {
        this.postBanService = postBanService;
    }

    @GetMapping("/ban-requests")
    public String getBanRequestsPage(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "10") int size,
            Model model) {

        PostBanRequestsViewModel viewModel = postBanService.getBanRequestList(page, size);
        model.addAttribute("banRequests", viewModel.getItems());
        model.addAttribute("totalCount", viewModel.getCount());
        model.addAttribute("currentPage", page);
        model.addAttribute("pageSize", size);
        model.addAttribute("totalPages", (int) Math.ceil((double) viewModel.getCount() / size));
        return "admin/ban-requests";
    }
}